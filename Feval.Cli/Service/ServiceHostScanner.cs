namespace Feval.Cli;

public class ServiceHostScanner
{
    public bool Scanning => m_UdpDetector.Detecting;

    public IReadOnlyList<Host> Hosts => m_Hosts;

    public event Action<Host> ServiceFound;

    public ServiceHostScanner()
    {
        m_UdpDetector = new UdpDetector(11111, 11112);
        m_UdpDetector.Start();
        m_Hosts = new List<Host>();
        m_UdpDetector.MessageReceived += OnMessageReceived;
    }

    public void Scan()
    {
        m_UdpDetector.Broadcast($"DETECTION-{UdpDetector.GetLocalIP()}");
    }

    private void OnMessageReceived(string message)
    {
        var words = message.Split('|');
        var deviceName = words[0];
        var ip = words[1];
        var port = int.Parse(words[2]);
        var host = new Host
        {
            deviceName = deviceName,
            ip = ip,
            port = port
        };
        m_Hosts.Add(host);
        ServiceFound(host);
    }

    private readonly UdpDetector m_UdpDetector;

    private readonly List<Host> m_Hosts;
}