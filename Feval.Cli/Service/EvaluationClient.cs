using System.Text;
using Net.Common;
using Net.Tcp.Client;

namespace Feval.Cli;

public sealed class EvaluationClient : IHandlerMessage
{
    public bool? Connected { get; private set; }
    
    public event Action ConnectFailed;

    public event Action Disconnected;

    public EvaluationClient(string address, int port)
    {
        m_Client = TCPClient.Create(this);
        m_Client.Connect(address, port);
    }

    public async Task<(bool, string)> EvaluateAsync(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (false, string.Empty);
        }

        if (input.StartsWith("#"))
        {
            await HandleMetaCommands(input[1..]);
            return (false, string.Empty);
        }

        var result = await EvaluateAsyncInternal(input);
        return (result != "$NoReturn", result);
    }

    public void HandleInitialize(IConnection connection)
    {
        m_Connection = connection;
    }

    public void HandleConnected(bool v)
    {
        if (!v)
        {
            ConnectFailed();
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
        Disconnected();
    }

    public void HandleClose()
    {
        Disconnected();
    }

    private async Task<string> EvaluateAsyncInternal(string input)
    {
        var data = Encoding.UTF8.GetBytes(input);
        m_Connection.Send(data, 0, data.Length);
        WaitingForResponse = true;
        await TaskUtility.WaitWhile(() => WaitingForResponse);
        return ReceivedMessage;
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
        else if (input.StartsWith("dpf"))
        {
            try
            {
                var words = input.Split(" ");
                var expression = words[1];
                var index = input.IndexOf(expression, StringComparison.Ordinal) + expression.Length + 1;
                var path = input[index..];

                var ret = await EvaluateAsync($"dump({expression})");
                Console.WriteLine(ret);

                await File.WriteAllTextAsync(path, ret.Item2);
                Console.WriteLine($"Dump result has been write to file: {path}");
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Invalid meta command: {input}");
            }
        }
        else
        {
            await Console.Error.WriteLineAsync($"Unsupported meta command: {input}");
        }
    }


    private bool WaitingForResponse { get; set; }

    private string ReceivedMessage { get; set; }

    private IConnection m_Connection;

    private readonly TCPClient m_Client;
}