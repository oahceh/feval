using System;
using System.Text;
using System.Threading.Tasks;
using Net.Common;
using Net.Tcp.Client;

namespace Feval.Cli
{
    internal sealed class EvaluationClient : IEvaluationService, IHandlerMessage, IDisposable
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

        public async Task Run(Options options)
        {
            m_Client = TCPClient.Create(this);
            m_Client.Connect(options.Address, options.Port);
            Console.WriteLine($"Connecting evaluation service: {options.Address}:{options.Port}");
            await TaskUtility.WaitUntil(() => Connected != null);
            if (Connected == false)
            {
                return;
            }

            Console.WriteLine($"Evaluation service connected: {options.Address}:{options.Port}");

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(">> ");
                Console.ResetColor();

                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
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

        private bool? Connected { get; set; }

        private bool WaitingForResponse { get; set; }

        private string ReceivedMessage { get; set; }

        private IConnection m_Connection;

        private TCPClient m_Client;
    }
}