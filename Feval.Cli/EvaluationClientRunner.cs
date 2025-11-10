using Spectre.Console;

namespace Feval.Cli;

internal sealed class EvaluationClientRunner : IEvaluationRunner
{
    public async Task Run(IOptionsManager manager)
    {
        OptionsManager = manager;
        var options = manager.Options;
        var runOptions = options.Run;

        var address = string.IsNullOrEmpty(runOptions.Address) ? "localhost" : runOptions.Address;
        var port = runOptions.Port;

        if (runOptions.Scan)
        {
            (address, port) = await ScanAndChooseServiceHostAsync();
            if (string.IsNullOrEmpty(address))
            {
                return;
            }
        }

        // Port fallback
        if (port == 0)
        {
            port = int.Parse(options.Configurations.GetValueOrDefault(
                ConfigurationKeys.DefaultPort,
                ConfigurationKeys.GetDefaultValue(ConfigurationKeys.DefaultPort))
            );
            AnsiConsole.WriteLine($"Use default port: {port}");
        }

        m_Client = new EvaluationClient(address, port);

        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Connecting...",
                async _ => { await TaskUtility.WaitUntil(() => m_Client.Connected != null); }
            );
        if (m_Client.Connected == false)
        {
            return;
        }

        AnsiConsole.Markup($"Evaluation service connected: [green]{address}:{port}\n[/]");

        if (options.DefaultUsingNamespaces.Count > 0)
        {
            Console.WriteLine("Using default namespaces:");
            foreach (var expression in options.DefaultUsingNamespaces.Select(ns => $"using {ns}"))
            {
                await m_Client.EvaluateAsync(expression);
                Console.WriteLine(expression);
            }
        }

        ReadLine.HistoryEnabled = true;
        ReadLine.AddHistory(options.History.ToArray());
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(">> ");
            Console.ResetColor();

            var input = ReadLine.Read();
            if (input.StartsWith("#"))
            {
                await HandleMetaCommands(input[1..]);
            }
            else
            {
                var (withResult, result) = await m_Client.EvaluateAsync(input);

                if (withResult)
                {
                    Console.WriteLine(result);
                }
            }
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
                        await m_Client.EvaluateAsync(line);
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

                var ret = await m_Client.EvaluateAsync($"dump({expression})");
                Console.WriteLine(ret);

                await File.WriteAllTextAsync(path, ret.Item2);
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

    private IOptionsManager OptionsManager { get; set; } = null!;

    private EvaluationClient m_Client = null!;
}