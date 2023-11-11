using System;
using System.Threading.Tasks;

namespace Feval.Cli
{
    internal sealed class EvaluationStandalone : IEvaluationService
    {
        public Task Run(Options option)
        {
            Context.Create();
            var context = Context.Main;
            context.WithReferences(AppDomain.CurrentDomain.GetAssemblies());
            // Print version info
            Console.WriteLine(context.Evaluate("version()"));
            // Cli Main Loop
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(">> ");
                Console.ResetColor();

                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                try
                {
                    var ret = context.Evaluate(line);
                    if (option.Verbose)
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
    }
}