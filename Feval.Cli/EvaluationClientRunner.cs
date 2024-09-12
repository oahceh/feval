using Spectre.Console;

namespace Feval.Cli
{
    internal sealed class EvaluationClientRunner : IEvaluationRunner
    {
        public async Task Run(IOptionsManager manager)
        {
            OptionsManager = manager;
            var options = manager.Options;
            var runOptions = options.Run;

            var address = runOptions.Address;
            var port = runOptions.Port;
            if (runOptions.Scan)
            {
                (address, port) = await ScanAndChooseServiceHost();
            }

            m_Client = new EvaluationClient(address, port);

            await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("Connecting...",
                async _ => { await TaskUtility.WaitUntil(() => m_Client.Connected != null); });
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

                var (withResult, result) = await m_Client.EvaluateAsync(ReadLine.Read());

                if (withResult)
                {
                    Console.WriteLine(result);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private async Task<(string, int)> ScanAndChooseServiceHost()
        {
            var scanner = new ServiceHostScanner();
            scanner.Scan();

            await AnsiConsole.Status().Spinner(Spinner.Known.Dots).StartAsync("Scanning...",
                async _ => { await TaskUtility.WaitWhile(() => scanner.Scanning); });

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

        public void Quit()
        {
            var allHistory = ReadLine.GetHistory();
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
}