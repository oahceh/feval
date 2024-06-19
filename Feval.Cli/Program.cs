using CommandLine;
using Newtonsoft.Json;
using Spectre.Console;
using Parser = CommandLine.Parser;

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

    [Verb("run", isDefault: true, HelpText = "Run in standalone mode")]
    internal sealed class RunOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
        public bool Verbose { get; set; }

        [Option('s', "standalone", Required = false, HelpText = "Running feval on standalone mode")]
        public bool Standalone { get; set; }

        [Value(0, MetaName = "address", HelpText = "Remote feval service address")]
        public string Address { get; set; }

        public int Port { get; set; }
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

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;
            OpsManager = new OptionsManager(OptionsPath);

            await Parser.Default.ParseArguments<RunOptions, UsingOptions, AliasOptions, HistoryOptions>(args)
                .MapResult(
                    (RunOptions options) => HandleRunOptions(options),
                    (UsingOptions options) => HandleUsingOptions(options),
                    (AliasOptions options) => HandleAliasOptions(options),
                    (HistoryOptions options) => HandleHistoryOptions(options),
                    _ => Task.FromResult(0)
                );
        }

        private static async Task HandleRunOptions(RunOptions options)
        {
            OpsManager.Options.Run = options;
            if (options.Verbose || string.IsNullOrEmpty(options.Address))
            {
                m_Runner = new EvaluationStandalone(OpsManager);
            }
            else
            {
                if (Ops.Aliases.TryGetValue(options.Address, out var aliasAddress))
                {
                    options.Address = aliasAddress;
                }

                if (!TryParseAddress(options.Address, out var address, out var port))
                {
                    AnsiConsole.Write(new Markup($"[red]Invalid address: {options.Address}[/]"));
                    return;
                }

                options.Address = address;
                options.Port = port;
                m_Runner = new EvaluationClient();
            }

            await m_Runner.Run(OpsManager);
        }

        private static bool TryParseAddress(string text, out string address, out int port)
        {
            address = string.Empty;
            port = 0;

            try
            {
                var words = text.Split(":");
                address = words.First();
                port = int.Parse(words.Last());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            m_Runner?.Quit();
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            m_Runner?.Quit();
        }

        private static async Task HandleUsingOptions(UsingOptions options)
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
                var table = new Table();
                table.AddColumn(new TableColumn("Index").Centered());
                table.AddColumn("Namespace");
                var index = 0;
                foreach (var ns in Ops.DefaultUsingNamespaces)
                {
                    table.AddRow($"{++index}", $"[green]{ns}[/]");
                }

                AnsiConsole.Write(table);
            }

            if (flag)
            {
                await OpsManager.WriteAsync();
            }
        }

        private static async Task HandleAliasOptions(AliasOptions options)
        {
            if (string.IsNullOrEmpty(options.Name) && string.IsNullOrEmpty(options.Address))
            {
                var table = new Table();
                table.AddColumn(new TableColumn("Alias").Centered());
                table.AddColumn("Address");
                foreach (var kv in Ops.Aliases)
                {
                    table.AddRow(kv.Key, $"[green]{kv.Value}[/]");
                }

                AnsiConsole.Write(table);
            }
            else if (!TryParseAddress(options.Address, out _, out _))
            {
                AnsiConsole.Write(new Markup($"[red]Invalid address: {options.Address}[/]"));
                return;
            }
            else
            {
                Ops.Aliases[options.Name] = options.Address;
                await OpsManager.WriteAsync();
            }
        }

        private static async Task HandleHistoryOptions(HistoryOptions options)
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
                await OpsManager.WriteAsync();
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

            public async Task WriteAsync()
            {
                try
                {
                    Directory.CreateDirectory(LocalDataDirectory);
                    await File.WriteAllTextAsync(SerializedPath,
                        JsonConvert.SerializeObject(Options, Formatting.Indented));
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