using System;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public abstract class TestCase
    {
        protected TestCase(ITestOutputHelper output)
        {
            m_Output = output;
            context = Context.Create(GetType().Name, false).WithReferences(AppDomain.CurrentDomain.GetAssemblies());
        }

        protected object Eval(string text)
        {
            m_Output.WriteInput(text);
            var ret = context.Evaluate(text);
            WithReturn = ret.WithReturn;
            retValue = ret.Value;
            if (WithReturn)
            {
                m_Output.WriteLine(retValue?.ToString() ?? "null");
            }

            return retValue;
        }

        protected void WriteLine(string text = "")
        {
            m_Output.WriteLine(text);
        }

        protected readonly Context context;

        private readonly ITestOutputHelper m_Output;

        protected object retValue;

        protected bool WithReturn { get; private set; }
    }
}