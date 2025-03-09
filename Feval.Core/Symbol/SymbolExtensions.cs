using System;

namespace Feval
{
    internal static class SymbolExtensions
    {
        public static EvaluationResult Invoke(this FunctionSymbol symbol, object[] args)
        {
            try
            {
                return new EvaluationResult(
                    symbol.MethodInfo.Invoke(null, args),
                    symbol.MethodInfo.ReturnType != typeof(void),
                    null
                );
            }
            catch (Exception e)
            {
                return EvaluationResult.FromException(e);
            }
        }
    }
}