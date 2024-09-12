using CommandLine;
using Newtonsoft.Json;
using Spectre.Console;
using Parser = CommandLine.Parser;

namespace Feval.Cli
{
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

        private static void OnServiceFound(Host obj)
        {
            Console.WriteLine($"Service Found: {obj}");
        }

        private static async Task HandleRunOptions(RunOptions options)
        {
            OpsManager.Options.Run = options;
            if (options.Scan)
            {
                m_Runner = new EvaluationClientRunner();
            }
            else if (options.Verbose || string.IsNullOrEmpty(options.Address))
            {
                m_Runner = new EvaluationStandalone(OpsManager);
            }
            else
            {
                if (Ops.Aliases.TryGetValue(options.Address, out var aliasAddress))
                {
                    options.Address = aliasAddress;
                }

                if (!options.TryParseAddress(out var error))
                {
                    AnsiConsole.Write(new Markup($"[red]{error}[/]"));
                    return;
                }

                m_Runner = new EvaluationClientRunner();
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
            else if (!RunOptions.TryParseAddress(options.Address, out var error, out _, out _))
            {
                AnsiConsole.Write(new Markup($"[red]{error}[/]"));
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

        private static IOptionsManager OpsManager { get; set; } = null!;

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
                catch (Exception)
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