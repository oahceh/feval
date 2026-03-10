using System;
using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class DelegateInvocationTests : TestCase
    {
        public DelegateInvocationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void InvokeVariableDelegate()
        {
            Eval("using Feval.UnitTests");
            Eval("using System");
            Eval("var holder = new DelegateHolder()");
            Eval("var func = holder.AddFunc");
            Eval("func(3, 4)");
            Assert.Equal(7, retValue);
        }

        [Fact]
        public void InvokeMemberDelegate()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("holder.AddFunc(10, 20)");
            Assert.Equal(30, retValue);
        }

        [Fact]
        public void InvokeVoidDelegate()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("holder.VoidAction(42)");
            Assert.False(WithReturn);
            Assert.Equal(42, DelegateHolder.LastActionValue);
        }

        [Fact]
        public void InvokeStaticMemberDelegate()
        {
            Eval("using Feval.UnitTests");
            Eval("DelegateHolder.StaticFunc(\"hello\")");
            Assert.Equal("HELLO", retValue);
        }

        [Fact]
        public void PassDelegateAsMethodArgument()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("holder.Apply(holder.AddFunc, 10, 20)");
            Assert.Equal(30, retValue);
        }

        [Fact]
        public void PassVariableDelegateAsMethodArgument()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("var func = holder.AddFunc");
            Eval("holder.Apply(func, 5, 6)");
            Assert.Equal(11, retValue);
        }

        [Fact]
        public void AccessDelegateFieldReturnsDelegate()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("holder.AddFunc");
            Assert.IsType<Func<int, int, int>>(retValue);
        }

        [Fact]
        public void PassDelegateToVoidMethod()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("holder.Execute(holder.VoidAction, 99)");
            Assert.False(WithReturn);
            Assert.Equal(99, DelegateHolder.LastActionValue);
        }

        [Fact]
        public void PassDelegateToStaticMethod()
        {
            Eval("using Feval.UnitTests");
            Eval("var holder = new DelegateHolder()");
            Eval("DelegateHolder.StaticApply(holder.AddFunc, 7, 8)");
            Assert.Equal(15, retValue);
        }
    }
}
