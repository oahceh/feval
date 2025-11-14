using System.Text;
using Net.Common;
using Net.Tcp.Client;

namespace Feval.Cli;

public sealed class EvaluationClient : IHandlerMessage
{
    public bool? Connected { get; private set; }

    public event Action Disconnected;

    public EvaluationClient(string address, int port)
    {
        m_Address = address;
        m_Port = port;
        m_Client = TCPClient.Create(this);
        m_Client.Connect(address, port);
    }

    public void Connect()
    {
        m_Client.Connect(m_Address, m_Port);
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

    private readonly string m_Address;

    private readonly int m_Port;
}