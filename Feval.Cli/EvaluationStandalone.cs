using Newtonsoft.Json;

namespace Feval.Cli
{
    internal sealed class EvaluationStandalone : IEvaluationRunner
    {
        public Task Run(IOptionsManager manager)
        {
            OptionsManager = manager;
            var options = manager.Options;
            var context = Context.Create();
            context.WithReferences(AppDomain.CurrentDomain.GetAssemblies());
            context.RegisterDumper(JsonConvert.SerializeObject);

            Console.WriteLine(context.Evaluate("version()"));
            Console.WriteLine(context.Evaluate("copyright()"));
            if (options.DefaultUsingNamespaces.Count > 0)
            {
                Console.WriteLine("Using default namespaces:");
                foreach (var expression in options.DefaultUsingNamespaces.Select(ns => $"using {ns}"))
                {
                    context.Evaluate(expression);
                    Console.WriteLine(expression);
                }
            }

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
                    var ret = context.Evaluate(line);
                    if (options.Default.Verbose)
                    {
                        Console.WriteLine("Tokens:");
                        Utility.PrintTokens(Console.Out, context.SyntaxTree.Tokens);
                        Console.WriteLine();

                        Console.WriteLine("Syntax Tree:");
                        Utility.PrettyPrint(Console.Out, context.SyntaxTree.Root);
                        Console.WriteLine();
                    }

                    if (ret.WithReturn)
                    {
                        Console.WriteLine(ret);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                }
            }
        }

        public void Quit()
        {
            var allHistory = ReadLine.GetHistory();
            var newHistory = allHistory.GetRange(OptionsManager.Options.History.Count,
                allHistory.Count - OptionsManager.Options.History.Count);
            if (OptionsManager.Options.AddHistory(newHistory))
            {
                OptionsManager.WriteOptions();
            }
        }

        private IOptionsManager OptionsManager { get; set; }
    }
}