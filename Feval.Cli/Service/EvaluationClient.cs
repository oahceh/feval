using System.IO;
using System.Text;
using Net.Common;
using Net.Tcp.Client;

namespace Feval.Cli;

public struct RemoteEvaluationResult
{
    public bool WithReturn;
    public string Value;
    public bool HasException;
    public string ExceptionMessage;
    public string ExceptionStackTrace;
}

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
        m_HandshakeCompleted = false;
        m_NewMsgPackSupported = false;
        m_Client.Connect(Address, Port);
    }

    public async Task NegotiateProtocolAsync()
    {
        var data = Encoding.UTF8.GetBytes(NewMsgPackMagicNumber.ToString());
        m_Connection.Send(data, 0, data.Length);
        WaitingForResponse = true;
        await TaskUtility.WaitWhile(() => WaitingForResponse);
    }

    public async Task<RemoteEvaluationResult> EvaluateAsync(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return default;
        }

        var data = Encoding.UTF8.GetBytes(input);
        m_Connection.Send(data, 0, data.Length);
        WaitingForResponse = true;
        await TaskUtility.WaitWhile(() => WaitingForResponse);
        return m_ReceivedResult;
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
        if (!m_HandshakeCompleted)
        {
            var text = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            m_NewMsgPackSupported = text == "true";
            m_HandshakeCompleted = true;
            WaitingForResponse = false;
            return;
        }

        if (m_NewMsgPackSupported)
        {
            using var ms = new MemoryStream(stream.GetBuffer(), 0, (int)stream.Length);
            using var reader = new BinaryReader(ms);
            m_ReceivedResult = new RemoteEvaluationResult
            {
                WithReturn = reader.ReadBoolean(),
                Value = reader.ReadString(),
                HasException = reader.ReadBoolean(),
                ExceptionMessage = reader.ReadString(),
                ExceptionStackTrace = reader.ReadString()
            };
        }
        else
        {
            var text = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            m_ReceivedResult = new RemoteEvaluationResult
            {
                WithReturn = text != NoReturnMarker,
                Value = text != NoReturnMarker ? text : string.Empty
            };
        }

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

    private bool WaitingForResponse { get; set; }

    private RemoteEvaluationResult m_ReceivedResult;

    private bool m_HandshakeCompleted;

    private bool m_NewMsgPackSupported;

    private IConnection m_Connection;

    private readonly TCPClient m_Client;

    private const long NewMsgPackMagicNumber = 11556654433221;

    private const string NoReturnMarker = "$NoReturn";
}
