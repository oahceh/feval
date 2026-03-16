using Spectre.Console;

namespace Feval.Cli;

internal sealed class EvaluationClientRunner : IEvaluationRunner
{
    public async Task Run(IOptionsManager manager)
    {
        OptionsManager = manager;
        var options = manager.Options;
        var runOptions = options.Run;

        string address;
        int port;
        if (runOptions.Scan)
        {
            (address, port) = await ScanAndChooseServiceHostAsync();
            if (string.IsNullOrEmpty(address))
            {
                return;
            }
        }
        else
        {
            if (options.Aliases.TryGetValue(runOptions.Address, out var aliasAddress))
            {
                runOptions.Address = aliasAddress;
            }

            if (!runOptions.TryParseAddress(out var error))
            {
                AnsiConsole.Write(new Markup($"[red]{error}[/]"));
                return;
            }

            address = string.IsNullOrEmpty(runOptions.Address) ? "localhost" : runOptions.Address;
            port = runOptions.Port;
        }

        // Port fallback
        if (port == 0)
        {
            port = int.Parse(options.Configurations.GetValueOrDefault(
                ConfigurationKeys.DefaultPort,
                ConfigurationKeys.GetDefaultValue(ConfigurationKeys.DefaultPort))
            );
            if (IsInteractive(runOptions))
            {
                AnsiConsole.WriteLine(string.Format(Locales.UseDefaultPort, port));
            }
        }

        m_Client = new EvaluationClient(address, port);
        m_Client.Disconnected += OnDisconnected;

        await ConnectAsync(runOptions);
        await NegotiateProtocolAsync();

        if (IsInteractive(runOptions))
        {
            await UsingDefaultNamespacesAsync(options.DefaultUsingNamespaces, verbose: true);
            ReadLine.HistoryEnabled = true;
            ReadLine.AddHistory(options.History.ToArray());
            m_IsInteractive = true;
            await Loop(options);
        }
        else
        {
            await UsingDefaultNamespacesAsync(options.DefaultUsingNamespaces, verbose: false);
            await RunNonInteractive(runOptions);
        }
    }

    private async Task ConnectAsync(RunOptions runOptions)
    {
        var interactive = IsInteractive(runOptions);
        while (true)
        {
            m_Client.Connect();
            await AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(Locales.Connecting,
                    async _ => { await TaskUtility.WaitUntil(() => m_Client.Connected != null); }
                );

            if (m_Client.Connected == null || !m_Client.Connected.Value)
            {
                if (!interactive)
                {
                    await Console.Error.WriteLineAsync("Failed to connect to remote service.");
                    Environment.ExitCode = 1;
                    return;
                }

                AnsiConsole.MarkupLine(string.Format(Locales.ServiceConnectFailed, "[green]ENTER[/]", "CTRL+C"));
                ReadLine.Read();
                continue;
            }

            if (interactive)
            {
                AnsiConsole.Markup(
                    string.Format(Locales.ServiceConnected, $"[green]{m_Client.Address}:{m_Client.Port}\n[/]")
                );
            }

            break;
        }
    }

    private async Task NegotiateProtocolAsync()
    {
        await m_Client.NegotiateProtocolAsync();
    }

    private async Task ReconnectAsync(Options options)
    {
        await ConnectAsync(options.Run);
        await NegotiateProtocolAsync();
        await UsingDefaultNamespacesAsync(options.DefaultUsingNamespaces, verbose: true);
        Reconnecting = false;
    }

    private async Task Loop(Options options)
    {
        while (true)
        {
            AnsiConsole.Markup("[green]>> [/]");
            var input = ReadLine.Read();

            if (m_Client.Connected == null || Reconnecting)
            {
                await ReconnectAsync(options);
            }

            if (input.StartsWith("#"))
            {
                await HandleMetaCommands(input[1..]);
            }
            else
            {
                var result = await m_Client.EvaluateAsync(input);
                PrintResult(result);
            }
        }
    }

    private async Task RunNonInteractive(RunOptions runOptions)
    {
        var hasExplicitInput = runOptions.Expressions.Any() || !string.IsNullOrEmpty(runOptions.ScriptFile);

        try
        {
            // 1. Execute -e expressions
            foreach (var expression in runOptions.Expressions)
            {
                if (!await EvaluateLineAsync(expression))
                {
                    Environment.ExitCode = 1;
                    return;
                }
            }

            // 2. Execute -f script file
            if (!string.IsNullOrEmpty(runOptions.ScriptFile))
            {
                if (!File.Exists(runOptions.ScriptFile))
                {
                    await Console.Error.WriteLineAsync($"Script file not found: {runOptions.ScriptFile}");
                    Environment.ExitCode = 1;
                    return;
                }

                foreach (var line in File.ReadLines(runOptions.ScriptFile))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (!await EvaluateLineAsync(line))
                        {
                            Environment.ExitCode = 1;
                            return;
                        }
                    }
                }
            }

            // 3. If no -e or -f, read from stdin
            if (!hasExplicitInput)
            {
                string? line;
                while ((line = Console.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (!await EvaluateLineAsync(line))
                        {
                            Environment.ExitCode = 1;
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Evaluates a single line via remote service. Returns true on success, false on error.
    /// </summary>
    private async Task<bool> EvaluateLineAsync(string line)
    {
        var result = await m_Client.EvaluateAsync(line);
        if (result.HasException)
        {
            await Console.Error.WriteLineAsync(result.ExceptionMessage);
            return false;
        }

        if (result.WithReturn)
        {
            Console.WriteLine(result.Value);
        }

        return true;
    }

    private async Task UsingDefaultNamespacesAsync(IReadOnlyList<string> namespaces, bool verbose)
    {
        if (namespaces.Count > 0)
        {
            if (verbose)
            {
                Console.WriteLine(Locales.UsingDefaultNamespaces);
            }

            foreach (var expression in namespaces.Select(ns => $"using {ns}"))
            {
                await m_Client.EvaluateAsync(expression);
                if (verbose)
                {
                    Console.WriteLine(expression);
                }
            }
        }
    }

    private void OnDisconnected()
    {
        if (m_IsInteractive)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(string.Format(Locales.ServiceDisconnected, "[green]ENTER[/]", "CTRL+C"));
            Reconnecting = true;
        }
    }

    private async Task<(string, int)> ScanAndChooseServiceHostAsync()
    {
        var scanner = new ServiceHostScanner();
        scanner.Scan();

        await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("Scanning...",
            async _ => { await TaskUtility.WaitWhile(() => scanner.Scanning); });

        if (scanner.Hosts.Count == 0)
        {
            AnsiConsole.Markup("[red]No services not found.[/]");
            return (string.Empty, 0);
        }

        var table = new Table();
        table.AddColumn("Index");
        table.AddColumn("Name");
        table.AddColumn("Address");

        var index = 0;
        foreach (var host in scanner.Hosts)
        {
            table.AddRow($"{++index}", host.deviceName, $"[green]{host.ip}:{host.port}[/]");
        }

        AnsiConsole.Write(table);
        while (true)
        {
            index = AnsiConsole.Ask<int>("Enter host index to connect: ");
            if (index > 0 && index <= scanner.Hosts.Count)
            {
                break;
            }

            AnsiConsole.Markup("[red]Invalid index, please try again.[/]");
        }

        return (scanner.Hosts[index - 1].ip, scanner.Hosts[index - 1].port);
    }

    private static void PrintResult(RemoteEvaluationResult result)
    {
        if (result.HasException)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(result.ExceptionMessage)}[/]");
            if (!string.IsNullOrEmpty(result.ExceptionStackTrace))
            {
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(result.ExceptionStackTrace)}[/]");
            }
        }
        else if (result.WithReturn)
        {
            AnsiConsole.WriteLine(result.Value);
        }
    }

    private async Task HandleMetaCommands(string input)
    {
        if (input.StartsWith("load"))
        {
            var path = input["load".Length..];
            path = new Uri(path).AbsolutePath;
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(">> ");
                        Console.ResetColor();
                        Console.WriteLine(line);
                        var result = await m_Client.EvaluateAsync(line);
                        PrintResult(result);
                    }
                }
            }
        }
        else if (input.StartsWith("dpf"))
        {
            try
            {
                var words = input.Split(" ");
                var expression = words[1];
                var index = input.IndexOf(expression, StringComparison.Ordinal) + expression.Length + 1;
                var path = input[index..];

                var result = await m_Client.EvaluateAsync($"dump({expression})");
                PrintResult(result);

                await File.WriteAllTextAsync(path, result.Value);
                Console.WriteLine($"Dump result has been write to file: {path}");
            }
            catch (Exception)
            {
                await Console.Error.WriteLineAsync($"UnInvalid meta command: {input}");
            }
        }
        else
        {
            await Console.Error.WriteLineAsync($"Unsupported meta command: {input}");
        }
    }

    public void Quit()
    {
        if (!m_IsInteractive)
        {
            return;
        }

        var allHistory = ReadLine.GetHistory();
        if (allHistory.Count == 0)
        {
            return;
        }

        var newHistory = allHistory.GetRange(OptionsManager.Options.History.Count,
            allHistory.Count - OptionsManager.Options.History.Count);
        if (OptionsManager.Options.AddHistory(newHistory))
        {
            OptionsManager.WriteAsync();
        }
    }

    private static bool IsInteractive(RunOptions runOptions)
    {
        if (runOptions.Expressions.Any() || !string.IsNullOrEmpty(runOptions.ScriptFile))
        {
            return false;
        }

        try
        {
            return !Console.IsInputRedirected;
        }
        catch
        {
            return true;
        }
    }

    private IOptionsManager OptionsManager { get; set; } = null!;

    private EvaluationClient m_Client = null!;

    private bool m_IsInteractive;

    private bool Reconnecting { get; set; }
}
