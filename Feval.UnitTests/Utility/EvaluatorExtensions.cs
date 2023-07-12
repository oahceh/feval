using System.Collections.Generic;

namespace Feval.UnitTests
{
    internal static class EvaluatorExtensions
    {
        internal static object Evaluate(this Context context, List<string> input)
        {
            object output = null;
            foreach (var i in input)
            {
                output = context.Evaluate(i);
            }

            return output;
        }
    }
}