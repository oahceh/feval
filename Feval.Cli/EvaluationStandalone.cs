using System.Reflection;
using Newtonsoft.Json;

namespace Feval.Cli;

internal sealed class EvaluationStandalone : IEvaluationRunner
{
    public EvaluationStandalone(IOptionsManager optionsManager)
    {
        OptionsManager = optionsManager;
    }

    public Task Run(IOptionsManager manager)
    {
        OptionsManager = manager;
        var options = manager.Options;
        var runOptions = options.Run;

        var context = Context.Create();
        context.WithReferences(AppDomain.CurrentDomain.GetAssemblies());
        context.RegisterDumper(obj => JsonConvert.SerializeObject(obj, Formatting.Indented));

        // Apply default using namespaces (silently in non-interactive mode)
        if (options.DefaultUsingNamespaces.Count > 0)
        {
            if (IsInteractive(runOptions))
            {
                Console.WriteLine("Using default namespaces:");
            }

            foreach (var expression in options.DefaultUsingNamespaces.Select(ns => $"using {ns}"))
            {
                context.Evaluate(expression);
                if (IsInteractive(runOptions))
                {
                    Console.WriteLine(expression);
                }
            }
        }

        if (IsInteractive(runOptions))
        {
            RunInteractive(context, options);
        }
        else
        {
            RunNonInteractive(context, runOptions);
        }

        return Task.CompletedTask;
    }

    public void Quit()
    {
        if (!m_IsInteractive)
        {
            return;
        }

        var allHistory = ReadLine.GetHistory();
        var newHistory = allHistory.GetRange(OptionsManager.Options.History.Count,
            allHistory.Count - OptionsManager.Options.History.Count);
        if (OptionsManager.Options.AddHistory(newHistory))
        {
            OptionsManager.WriteAsync();
        }
    }

    private void RunInteractive(Context context, Options options)
    {
        m_IsInteractive = true;
        var runOptions = options.Run;

        context.RegisterBuiltInFunction("quit",
            typeof(EvaluationStandalone).GetMethod(nameof(QuitInternal),
                BindingFlags.Static | BindingFlags.NonPublic));

        Console.WriteLine(context.Evaluate("version()"));
        Console.WriteLine(context.Evaluate("copyright()"));

        ReadLine.HistoryEnabled = true;
        ReadLine.AddHistory(options.History.ToArray());

        // Cli Main Loop
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(">> ");
            Console.ResetColor();

            var line = ReadLine.Read();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            try
            {
                EvaluateLine(context, runOptions, line, Console.Out);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ResetColor();
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private static void RunNonInteractive(Context context, RunOptions runOptions)
    {
        var hasExplicitInput = runOptions.Expressions.Any() || !string.IsNullOrEmpty(runOptions.ScriptFile);

        try
        {
            // 1. Execute -e expressions
            foreach (var expression in runOptions.Expressions)
            {
                if (!EvaluateLine(context, runOptions, expression, Console.Out))
                {
                    Environment.ExitCode = 1;
                    return;
                }
            }

            // 2. Execute -f script file
            if (!string.IsNullOrEmpty(runOptions.ScriptFile))
            {
                if (!File.Exists(runOptions.ScriptFile))
                {
                    Console.Error.WriteLine($"Script file not found: {runOptions.ScriptFile}");
                    Environment.ExitCode = 1;
                    return;
                }

                foreach (var line in File.ReadLines(runOptions.ScriptFile))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (!EvaluateLine(context, runOptions, line, Console.Out))
                        {
                            Environment.ExitCode = 1;
                            return;
                        }
                    }
                }
            }

            // 3. If no -e or -f, read from stdin
            if (!hasExplicitInput)
            {
                string? line;
                while ((line = Console.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (!EvaluateLine(context, runOptions, line, Console.Out))
                        {
                            Environment.ExitCode = 1;
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Evaluates a single line. Returns true on success, false on error.
    /// </summary>
    private static bool EvaluateLine(Context context, RunOptions runOptions, string line, TextWriter output)
    {
        var ret = context.Evaluate(line);
        if (ret.Exception != null)
        {
            Console.Error.WriteLine(ret.Exception.Message);
            return false;
        }

        if (runOptions.Verbose)
        {
            output.WriteLine("Tokens:");
            Utility.PrintTokens(output, context.SyntaxTree.Tokens);
            output.WriteLine();

            output.WriteLine("Syntax Tree:");
            Utility.PrettyPrint(output, context.SyntaxTree.Root);
            output.WriteLine();
        }

        if (ret.WithReturn)
        {
            output.WriteLine(ret);
        }

        return true;
    }

    private static bool IsInteractive(RunOptions runOptions)
    {
        if (runOptions.Expressions.Any() || !string.IsNullOrEmpty(runOptions.ScriptFile))
        {
            return false;
        }

        try
        {
            return !Console.IsInputRedirected;
        }
        catch
        {
            return true;
        }
    }

    private static void QuitInternal()
    {
        Environment.Exit(0);
    }

    private bool m_IsInteractive;
    private IOptionsManager OptionsManager { get; set; }
}
