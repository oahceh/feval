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

            await Parser.Default.ParseArguments<RunOptions, UsingOptions, AliasOptions, ConfigOptions>(args)
                .MapResult(
                    (RunOptions options) => HandleRunOptions(options),
                    (UsingOptions options) => HandleUsingOptions(options),
                    (AliasOptions options) => HandleAliasOptions(options),
                    (ConfigOptions options) => HandleConfigOptions(options),
                    _ => Task.FromResult(0)
                );
        }

        private static async Task HandleRunOptions(RunOptions options)
        {
            OpsManager.Options.Run = options;
            if (options.Standalone)
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

        private static async Task HandleConfigOptions(ConfigOptions options)
        {
            if (options.List)
            {
                var table = new Table();
                table.AddColumn(new TableColumn("Key").Centered());
                table.AddColumn("Value");
                foreach (var kv in Ops.Configurations)
                {
                    table.AddRow(kv.Key, $"[green]{kv.Value}[/]");
                }

                AnsiConsole.Write(table);
                return;
            }

            switch (options.Key)
            {
                case ConfigurationKeys.MaxHistory:
                    if (!int.TryParse(options.Value, out _))
                    {
                        AnsiConsole.Write(new Markup($"[red]Invalid max history number: {options.Key}[/]"));
                        return;
                    }

                    break;
                case ConfigurationKeys.DefaultPort:
                    if (!int.TryParse(options.Value, out _))
                    {
                        AnsiConsole.Write(new Markup($"[red]Invalid port number: {options.Key}[/]"));
                        return;
                    }

                    break;
                default:
                    AnsiConsole.Write(new Markup($"[red]Invalid configuration key: {options.Key}[/]"));
                    AnsiConsole.Write(ConfigurationKeys.GetHelpText());
                    return;
            }

            Ops.Configurations[options.Key] = options.Value;
            await OpsManager.WriteAsync();
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
            else if (!options.Address.IsValidIPAddress())
            {
                AnsiConsole.Write(new Markup($"[red]Invalid address[/]"));
            }
            else
            {
                Ops.Aliases[options.Name] = options.Address;
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