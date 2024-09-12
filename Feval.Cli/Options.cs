using CommandLine;
using Newtonsoft.Json;

namespace Feval.Cli
{
    internal interface IOptionsManager
    {
        Options Options { get; }

        Task WriteAsync();
    }

    [Verb("history", HelpText = "Set history options")]
    internal sealed class HistoryOptions
    {
        [Option('m', "max", Required = false, HelpText = "Max history count")]
        public int MaxCount { get; set; } = -1;
    }

    [Verb("alias", HelpText = "Remote feval service address aliases")]
    internal sealed class AliasOptions
    {
        [Value(0, MetaName = "name", HelpText = "Alias name")]
        public string Name { get; set; }

        [Value(1, MetaName = "address", HelpText = "Alias name")]
        public string Address { get; set; }
    }

    [Verb("using", HelpText = "Set default using namespaces on launch")]
    internal sealed class UsingOptions
    {
        [Option('a', "add", Required = false, HelpText = "Add default using namespaces")]
        public IEnumerable<string> AddingNamespaces { get; set; }

        [Option('r', "remove", Required = false, HelpText = "Remove default using namespaces")]
        public IEnumerable<string> RemovingNamespaces { get; set; }

        [Option('c', "clear", Required = false, HelpText = "Clear all default using namespaces")]
        public bool Clear { get; set; }
    }

    [Verb("run", isDefault: true, HelpText = "Running in standalone mode or connect a remote service")]
    internal sealed class RunOptions
    {
        [Option('s', "scan", Required = false,
            HelpText = "Scan all available service host in local network automatically")]
        public bool Scan { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
        public bool Verbose { get; set; }

        [Value(0, MetaName = "address", HelpText = $"Remote service address formatted like: {AddressTemplate}")]
        public string Address { get; set; } = string.Empty;

        public int Port { get; private set; }

        public bool TryParseAddress(out string error)
        {
            var ret = TryParseAddress(Address, out error, out var address, out var port);
            Address = address;
            Port = port;
            return ret;
        }

        public static bool TryParseAddress(string rawAddress, out string error, out string address, out int port)
        {
            error = string.Empty;
            address = string.Empty;
            port = 0;
            try
            {
                var words = rawAddress.Split(":");
                address = words[0];
                port = int.Parse(words[1]);
                return true;
            }
            catch (Exception)
            {
                error = InvalidAddressError(rawAddress);
                return false;
            }
        }

        private static string InvalidAddressError(string address)
        {
            return $"Invalid address: '{address}', please enter a valid address formatted like: {AddressTemplate}";
        }

        private const string AddressTemplate = "127.0.0.1:9999";
    }

    internal sealed class Options
    {
        [JsonProperty("namespaces")]
        public List<string> DefaultUsingNamespaces { get; set; } = new();

        [JsonProperty("history")]
        public List<string> History { get; set; } = new();

        [JsonProperty("max_history")]
        public int MaxHistoryCount { get; set; } = 20;

        [JsonProperty("aliases")]
        public Dictionary<string, string> Aliases { get; set; } = new();

        [JsonIgnore]
        public RunOptions Run { get; set; }

        public bool AddHistory(List<string> history)
        {
            history.RemoveAll(string.IsNullOrEmpty);
            EnsureSize(history, MaxHistoryCount);

            var changed = false;
            if (history.Count > 0)
            {
                History.AddRange(history);
                EnsureSize(History, MaxHistoryCount);
                changed = true;
            }

            return changed;
        }

        public Options StripEmptyHistory()
        {
            History.RemoveAll(string.IsNullOrEmpty);
            return this;
        }

        public Options EnsureHistoryCount()
        {
            EnsureSize(History, MaxHistoryCount);
            return this;
        }

        private static void EnsureSize(List<string> items, int count)
        {
            var diff = items.Count - count;
            if (diff > 0)
            {
                items.RemoveRange(0, diff);
            }
        }
    }
}