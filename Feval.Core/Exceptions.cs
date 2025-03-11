using System;

namespace Feval
{
    internal abstract class NonStackTraceException : Exception
    {
        protected NonStackTraceException(string message) : base(message)
        {
        }

        public override string StackTrace => string.Empty;
    }

    internal sealed class SyntaxErrorException : NonStackTraceException
    {
        public SyntaxErrorException(string message) : base(message)
        {
        }
    }

    internal sealed class MemberNotFoundException : NonStackTraceException
    {
        public MemberNotFoundException(string member) : base($"Member '{member}' not found")
        {
        }
    }

    internal sealed class SymbolNotFoundException : NonStackTraceException
    {
        public SymbolNotFoundException(string symbol) : base($"Symbol '{symbol}' not found")
        {
        }
    }

    internal sealed class MethodNotFoundException : NonStackTraceException
    {
        public MethodNotFoundException(string method) : base($"Method '{method}' not found")
        {
        }
    }

    internal sealed class TypeNotFoundException : NonStackTraceException
    {
        public TypeNotFoundException(string type) : base($"Type '{type}' not found")
        {
        }
    }

    internal sealed class GenericMethodNotFoundException : NonStackTraceException
    {
        public GenericMethodNotFoundException(string method) : base($"Generic method '{method}' not found")
        {
        }
    }

    internal sealed class EvaluateException : NonStackTraceException
    {
        public EvaluateException(string message) : base(message)
        {
        }
    }
}