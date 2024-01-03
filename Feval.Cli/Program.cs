using CommandLine;
using Newtonsoft.Json;
using Parser = CommandLine.Parser;

namespace Feval.Cli
{
    internal interface IOptionsManager
    {
        Options Options { get; }

        void WriteOptions();
    }

    [Verb("history", HelpText = "Set history options")]
    internal sealed class HistoryOptions
    {
        [Option('m', "max", Required = false, HelpText = "Max history count")]
        public int MaxCount { get; set; } = -1;
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

    [Verb("connect", HelpText = "Connect a remote evaluation service")]
    internal sealed class ConnectOptions
    {
        [Option('a', "address", Required = true, HelpText = "The remote feval evaluation service address")]
        public string Address { get; set; }

        [Option('p', "port", Required = false, HelpText = "The remote feval evaluation service port", Default = 9999)]
        public int Port { get; set; }
    }

    [Verb("run", isDefault: true, HelpText = "Run in standalone mode")]
    internal sealed class DefaultOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
        public bool Verbose { get; set; }
    }

    internal sealed class Options
    {
        [JsonProperty("namespaces")]
        public List<string> DefaultUsingNamespaces { get; set; } = new();

        [JsonProperty("history")]
        public List<string> History { get; set; } = new();

        [JsonProperty("max_history")]
        public int MaxHistoryCount { get; set; } = 20;

        [JsonIgnore]
        public DefaultOptions Default { get; set; }

        [JsonIgnore]
        public ConnectOptions Connect { get; set; }

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

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;
            OpsManager = new OptionsManager(OptionsPath);
            Parser.Default.ParseArguments<DefaultOptions, ConnectOptions, UsingOptions, HistoryOptions>(args)
                .WithParsed<DefaultOptions>(options =>
                {
                    OpsManager.Options.Default = options;
                    m_Runner = new EvaluationStandalone();
                })
                .WithParsed<ConnectOptions>(options =>
                {
                    OpsManager.Options.Connect = options;
                    m_Runner = new EvaluationClient();
                })
                .WithParsed<UsingOptions>(HandleUsingOptions)
                .WithParsed<HistoryOptions>(HandleHistoryOptions);

            if (m_Runner == null)
            {
                return;
            }

            await m_Runner.Run(OpsManager);
        }

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            m_Runner?.Quit();
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            m_Runner?.Quit();
        }

        private static void HandleUsingOptions(UsingOptions options)
        {
            var flag = false;
            if (options.AddingNamespaces.Any())
            {
                Ops.DefaultUsingNamespaces.AddRange(options.AddingNamespaces);
                flag = true;
            }
            else if (options.RemovingNamespaces.Any())
            {
                foreach (var ns in options.RemovingNamespaces)
                {
                    Ops.DefaultUsingNamespaces.Remove(ns);
                    flag = true;
                }
            }
            else if (options.Clear)
            {
                Ops.DefaultUsingNamespaces.Clear();
                flag = true;
            }
            else
            {
                foreach (var ns in Ops.DefaultUsingNamespaces)
                {
                    Console.WriteLine(ns);
                }
            }

            if (flag)
            {
                OpsManager.WriteOptions();
            }
        }

        private static void HandleHistoryOptions(HistoryOptions options)
        {
            if (options.MaxCount <= 0)
            {
                if (Ops.History.Count > 0)
                {
                    Console.WriteLine("Input history:");
                    foreach (var history in Ops.History)
                    {
                        Console.WriteLine(history);
                    }
                }
            }
            else
            {
                Ops.MaxHistoryCount = options.MaxCount;
                OpsManager.WriteOptions();
            }
        }

        private static IEvaluationRunner? m_Runner;

        private static string LocalDataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Feval.Cli");

        private static string OptionsPath => Path.Combine(LocalDataDirectory, "options.json");

        private static Options Ops => OpsManager.Options;

        private static IOptionsManager OpsManager { get; set; }

        private class OptionsManager : IOptionsManager
        {
            public OptionsManager(string path)
            {
                SerializedPath = path;
                Options = ReadOptions();
            }

            public Options Options { get; }

            private Options ReadOptions()
            {
                Options? options = null;
                try
                {
                    if (File.Exists(SerializedPath))
                    {
                        var text = File.ReadAllText(SerializedPath);
                        options = JsonConvert.DeserializeObject<Options>(text);
                    }
                }
                catch (Exception e)
                {
                    options = new Options();
                }

                var ret = options ?? new Options();
                ret.StripEmptyHistory().EnsureHistoryCount();
                return ret;
            }

            public void WriteOptions()
            {
                try
                {
                    if (!Directory.Exists(LocalDataDirectory))
                    {
                        Directory.CreateDirectory(LocalDataDirectory);
                    }

                    File.WriteAllText(SerializedPath, JsonConvert.SerializeObject(Options, Formatting.Indented));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            private string SerializedPath { get; }
        }
    };
}