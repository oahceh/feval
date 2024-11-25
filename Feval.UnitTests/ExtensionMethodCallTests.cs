using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class ExtensionMethodCallTests : TestCase
    {
        public ExtensionMethodCallTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ExtensionMethodCall()
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

        [Fact]
        public void GenericExtensionMethodCall()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("list = new List<int>()");
            Eval("list.Add(1)");
            Eval("list.Add(2)");
            Eval("list.First()");
            Assert.Equal(1, retValue);
        }
    }
}