using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class InstanceMethodCall
    {
        public int IntValue { get; private set; }

        public string StringValue { get; private set; }

        public void SetValue(int value = 100)
        {
            IntValue = value;
        }

        public void SetValue(string stringValue, int intValue = 100)
        {
            StringValue = stringValue;
            IntValue = intValue;
        }
    }

    public static class InstanceMethodCallExtensions
    {
        public static string GetStringValue(this InstanceMethodCall instance)
        {
            instance.SetValue("Hello World");
            return instance.StringValue;
        }

        public static string GetStringValue(this InstanceMethodCall instance, string newValue)
        {
            instance.SetValue(newValue);
            return instance.StringValue;
        }
    }

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

        [Fact]
        public void TestExtensionMethod()
        {
            Eval("using Feval.UnitTests");
            Eval("var instance = new InstanceMethodCall()");
            WriteLine();

            Eval("instance.GetStringValue()");
            Assert.Equal("Hello World", retValue);
            WriteLine();

            Eval("instance.GetStringValue(\"Hello Extensions\")");
            Assert.Equal("Hello Extensions", retValue);
        }
    }
}