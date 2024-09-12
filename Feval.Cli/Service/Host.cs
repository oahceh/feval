namespace Feval.Cli;

[Serializable]
public class Host
{
    public static Host Local => new Host
    {
        ip = "127.0.0.1",
    };

    public string deviceName;

    public string ip;

    public int port;

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            return $"{deviceName}({ip}:{port})";
        }

        return $"{ip}:{port}";
    }
}