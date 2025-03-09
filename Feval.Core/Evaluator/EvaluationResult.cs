using System;

namespace Feval
{
    public readonly struct EvaluationResult
    {
        public bool WithReturn { get; }

        public object Value { get; }

        public Exception Exception { get; }

        public EvaluationResult(object value, bool withReturn, Exception exception)
        {
            Value = value;
            WithReturn = withReturn;
            Exception = exception;
        }

        public static EvaluationResult FromValue(object value)
        {
            return new EvaluationResult(value, true, null);
        }

        public static EvaluationResult FromException(Exception exception)
        {
            return new EvaluationResult(null, false, exception);
        }

        public static readonly EvaluationResult Void = new(null, false, null);

        public override string ToString()
        {
            if (Value == null)
            {
                return "null";
            }

            if (Value is string str)
            {
                return $"\"{str}\"";
            }

            return Value.ToString();
        }
    }
}