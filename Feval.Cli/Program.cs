using System.Threading.Tasks;
using CommandLine;
using Parser = CommandLine.Parser;

namespace Feval.Cli
{
    internal sealed class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages")]
        public bool Verbose { get; set; }

        [Option('a', "address", Required = false, HelpText = "The remote feval evaluation service address")]
        public string Address { get; set; }

        [Option('p', "port", Required = false, HelpText = "The remote feval evaluation service port", Default = 9999)]
        public int Port { get; set; }
    }

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).Value;
            if (options == null)
            {
                return;
            }

            m_Service = string.IsNullOrEmpty(options.Address)
                ? new EvaluationStandalone() as IEvaluationService
                : new EvaluationClient();
            await m_Service.Run(options);
        }

        private static IEvaluationService m_Service;
    };
}