using System.Reflection;
using System.Text.Json.Serialization;
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

    [Verb("config", HelpText = "Config feval command line tool")]
    internal sealed class ConfigOptions
    {
        [Value(0, MetaName = "key", Required = false, HelpText = "Configuration key")]
        public string Key { get; set; }

        [Value(1, MetaName = "value", Required = false, HelpText = "Configuration value")]
        public string Value { get; set; }

        [Option('l', "list", Required = false, HelpText = "List all configurations")]
        public bool List { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ConfigurationKeyAttribute : Attribute
    {
        public string HelpText { get; set; }
    }

    internal static class ConfigurationKeys
    {
        [ConfigurationKey(HelpText = "Default remote service port")]
        public const string DefaultPort = "port.default";

        [ConfigurationKey(HelpText = "Max history count")]
        public const string MaxHistory = "history.max";

        public static List<string> All()
        {
            return typeof(ConfigurationKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string) &&
                                field is { IsLiteral: true, IsInitOnly: false } &&
                                Attribute.IsDefined(field, typeof(ConfigurationKeyAttribute)))
                .Select(prop => prop.GetValue(null) as string)
                .Where(value => !string.IsNullOrEmpty(value))
                .ToList()!;
        }

        public static string GetHelpText()
        {
            var helpTexts = typeof(ConfigurationKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field =>
                    field.FieldType == typeof(string) && Attribute.IsDefined(field, typeof(ConfigurationKeyAttribute)))
                .Select(field => new
                {
                    Key = field.GetValue(null) as string,
                    Text = ((ConfigurationKeyAttribute) Attribute.GetCustomAttribute(field,
                        typeof(ConfigurationKeyAttribute)))?.HelpText
                })
                .Where(item => !string.IsNullOrEmpty(item.Key) && !string.IsNullOrEmpty(item.Text))
                .ToList();

            return string.Join(Environment.NewLine, helpTexts.Select(item => $"{item.Key}: {item.Text}"));
        }
    }

    [Verb("run", isDefault: true, HelpText = "Running in standalone mode or connect a remote service")]
    internal sealed class RunOptions
    {
        [Option("standalone", Required = false, HelpText = "Running in standalone mode")]
        public bool Standalone { get; set; }

        [Option('s', "scan", Required = false,
            HelpText = "Scan all available service host in local network automatically")]
        public bool Scan { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages (standalone mode only)")]
        public bool Verbose { get; set; }

        [Value(0, MetaName = "address", HelpText = $"Remote service address formatted like: {AddressTemplate}")]
        public string Address { get; set; } = string.Empty;

        public int Port { get; private set; }

        public bool TryParseAddress(out string error)
        {
            var ret = Address.TryParseIPAddress(out var address, out var port);
            error = "Invalid address";
            Address = address;
            Port = port;
            return ret;
        }

        private const string AddressTemplate = "127.0.0.1:9999";
    }

    internal sealed class Options
    {
        [JsonProperty("namespaces")]
        public List<string> DefaultUsingNamespaces { get; set; } = new();

        [JsonProperty("history")]
        public List<string> History { get; set; } = new();

        public int MaxHistoryCount => int.Parse(Configurations.GetValueOrDefault(ConfigurationKeys.MaxHistory, "20"));

        [JsonProperty("aliases")]
        public Dictionary<string, string> Aliases { get; set; } = new();

        [JsonProperty("configurations")]
        public Dictionary<string, string> Configurations { get; set; } = new();

        [Newtonsoft.Json.JsonIgnore]
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