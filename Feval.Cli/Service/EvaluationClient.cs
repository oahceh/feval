using System.Text;
using Net.Common;
using Net.Tcp.Client;

namespace Feval.Cli;

public sealed class EvaluationClient : IHandlerMessage
{
    public string Address { get; }

    public int Port { get; }
    
    public bool? Connected { get; private set; }

    public event Action Disconnected;

    public EvaluationClient(string address, int port)
    {
        Address = address;
        Port = port;
        m_Client = TCPClient.Create(this);
    }

    public void Connect()
    {
        m_Client.Connect(Address, Port);
    }

    public async Task<(bool, string)> EvaluateAsync(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
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
        Connected = v;
    }

    public void Handle(PooledMemoryStream stream)
    {
        ReceivedMessage = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int) stream.Length);
        WaitingForResponse = false;
    }

    public void HandleDisconnected()
    {
        Connected = null;
        Disconnected?.Invoke();
    }

    public void HandleClose()
    {
        Connected = null;
    }

    private async Task<string> EvaluateAsyncInternal(string input)
    {
        var data = Encoding.UTF8.GetBytes(input);
        m_Connection.Send(data, 0, data.Length);
        WaitingForResponse = true;
        await TaskUtility.WaitWhile(() => WaitingForResponse);
        return ReceivedMessage;
    }


    private bool WaitingForResponse { get; set; }

    private string ReceivedMessage { get; set; }

    private IConnection m_Connection;

    private readonly TCPClient m_Client;
}