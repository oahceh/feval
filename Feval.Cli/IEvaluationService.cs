using System.Threading.Tasks;

namespace Feval.Cli
{
    internal interface IEvaluationService
    {
        Task Run(Options options);
    }
}