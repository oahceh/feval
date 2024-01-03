namespace Feval.Cli
{
    internal interface IEvaluationRunner
    {
        Task Run(IOptionsManager options);

        void Quit();
    }
}