using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class InstanceMethodCallTests : TestCase
    {
        public InstanceMethodCallTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            Eval("using Feval.UnitTests");
            Eval("var instance = new InstanceMethodCall()");
            WriteLine();

            Eval("instance.SetValue()");
            Assert.False(WithReturn);
            Eval("instance.IntValue");
            Assert.Equal(100, retValue);
            WriteLine();

            Eval("instance.SetValue(1)");
            Eval("instance.IntValue");
            Assert.Equal(1, retValue);
            WriteLine();

            Eval("instance.SetValue(-100)");
            Eval("instance.IntValue");
            Assert.Equal(-100, retValue);
            WriteLine();

            Eval("instance.SetValue(\"Hello World\")");
            Eval("instance.StringValue");
            Assert.Equal("Hello World", retValue);
            Eval("instance.IntValue");
            Assert.Equal(100, retValue);
            WriteLine();

            Eval("instance.SetValue(\"Hello World\", 999)");
            Eval("instance.StringValue");
            Assert.Equal("Hello World", retValue);
            Eval("instance.IntValue");
            Assert.Equal(999, retValue);
        }
    }
}