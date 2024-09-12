using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Feval.Cli;

public sealed class UdpDetector
{
    #region Property

    public event Action<string> MessageReceived;

    public bool Detecting { get; private set; }

    #endregion

    #region Interface

    public UdpDetector(int localPort, int remotePort)
    {
        m_UdpClient = new UdpClient(localPort);
        m_RemotePort = remotePort;
    }

    public void Start()
    {
        m_TokenSource = new CancellationTokenSource();
        Task.Run(Run, m_TokenSource.Token);
    }

    public static string? GetLocalIP() => Dns.GetHostEntry(Dns.GetHostName()).AddressList
        .FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork)?.ToString();

    public static string? FirstTwoSegmentOfLocalIP
    {
        get
        {
            var words = GetLocalIP()?.Split('.');
            if (words == null || words.Length < 2)
            {
                return null;
            }

            return $"{words[0]}.{words[1]}";
        }
    }

    public void Broadcast(string message)
    {
        if (Detecting)
        {
            return;
        }

        // Task.Run(() => Detect(message));
        Detect(message);
    }

    public void Send(string message, string ip, int port)
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        var buffer = Encoding.UTF8.GetBytes(message);
        m_UdpClient.Send(buffer, buffer.Length, endPoint);
    }

    public void Stop()
    {
        m_TokenSource?.Cancel();
        m_UdpClient?.Close();
    }

    #endregion

    #region Method

    private void Run()
    {
        var remotePoint = new IPEndPoint(IPAddress.Any, m_RemotePort);
        while (true)
        {
            if (m_TokenSource.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var buffer = m_UdpClient.Receive(ref remotePoint);
                OnReceiveMessage(buffer);
            }
            catch (Exception e)
            {
            }

            Thread.Sleep(200);
        }
    }

    private void OnReceiveMessage(byte[] data)
    {
        if (!Detecting)
        {
            return;
        }

        var text = Encoding.UTF8.GetString(data, 0, data.Length);
        MessageReceived?.Invoke(text);
    }

    private async void Detect(string message)
    {
        Detecting = true;
        var subnetAddress = FirstTwoSegmentOfLocalIP;
        var buffer = Encoding.UTF8.GetBytes(message);
        // 公司UDP组播貌似受限, 先临时用这种暴力方式吧
        for (var i = 0; i < byte.MaxValue; i++)
        {
            for (var j = 0; j < byte.MaxValue; j++)
            {
                var endPoint = new IPEndPoint(IPAddress.Parse($"{subnetAddress}.{i}.{j}"), m_RemotePort);
                await m_UdpClient.SendAsync(buffer, buffer.Length, endPoint);
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(1));

        Detecting = false;
    }

    #endregion

    #region Field

    private readonly UdpClient m_UdpClient;

    private readonly int m_RemotePort;

    private CancellationTokenSource m_TokenSource;

    #endregion
}