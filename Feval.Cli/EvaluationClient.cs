using System.Text;
using Net.Common;
using Net.Tcp.Client;

namespace Feval.Cli
{
    internal sealed class EvaluationClient : IEvaluationRunner, IHandlerMessage, IDisposable
    {
        public void HandleInitialize(IConnection connection)
        {
            m_Connection = connection;
        }

        public void HandleConnected(bool v)
        {
            if (!v)
            {
                Console.WriteLine("Failed to connect");
                Environment.Exit(1);
                return;
            }

            Connected = v;
        }

        public void Handle(PooledMemoryStream stream)
        {
            ReceivedMessage = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int) stream.Length);
            WaitingForResponse = false;
        }

        public void HandleDisconnected()
        {
            Environment.Exit(0);
        }

        public void HandleClose()
        {
            Environment.Exit(0);
        }

        public async Task Run(IOptionsManager manager)
        {
            OptionsManager = manager;
            var options = manager.Options;
            var connectOptions = options.Connect;
            m_Client = TCPClient.Create(this);
            m_Client.Connect(connectOptions.Address, connectOptions.Port);
            Console.WriteLine($"Connecting evaluation service: {connectOptions.Address}:{connectOptions.Port}...");
            await TaskUtility.WaitUntil(() => Connected != null);
            if (Connected == false)
            {
                return;
            }

            Console.WriteLine($"Evaluation service connected: {connectOptions.Address}:{connectOptions.Port}");

            if (options.DefaultUsingNamespaces.Count > 0)
            {
                Console.WriteLine("Using default namespaces:");
                foreach (var expression in options.DefaultUsingNamespaces.Select(ns => $"using {ns}"))
                {
                    await EvaluateAsync(expression);
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
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.StartsWith("#"))
                {
                    await HandleMetaCommands(input[1..]);
                    continue;
                }

                var result = await EvaluateAsync(input);
                if (result == "$NoReturn")
                {
                    continue;
                }

                Console.WriteLine(result);
            }
            // ReSharper disable once FunctionNeverReturns
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
                            await EvaluateAsync(line);
                        }
                    }
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
            var newHistory = allHistory.GetRange(OptionsManager.Options.History.Count,
                allHistory.Count - OptionsManager.Options.History.Count);
            if (OptionsManager.Options.AddHistory(newHistory))
            {
                OptionsManager.WriteOptions();
            }
        }

        public void Dispose()
        {
            m_Client?.Close();
        }

        private async Task<string> EvaluateAsync(string input)
        {
            var data = Encoding.UTF8.GetBytes(input);
            m_Connection.Send(data, 0, data.Length);
            WaitingForResponse = true;
            await TaskUtility.WaitWhile(() => WaitingForResponse);
            return ReceivedMessage;
        }

        private IOptionsManager OptionsManager { get; set; }

        private bool? Connected { get; set; }

        private bool WaitingForResponse { get; set; }

        private string ReceivedMessage { get; set; }

        private IConnection m_Connection;

        private TCPClient m_Client;
    }
}