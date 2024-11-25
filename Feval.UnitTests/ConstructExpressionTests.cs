using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public sealed class ConstructExpressionTests : TestCase
    {
        public ConstructExpressionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void NonGenericType()
        {
            Eval("var instance = new Feval.UnitTests.AddTest(1, 2)");
            Eval("instance.Add()");
            Assert.Equal(3, retValue);
        }

        [Fact]
        public void GenericListWithBaseType()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("list = new List<string>()");
            Eval("list.Add(\"Hello World\")");
            Eval("list[0]");
            Assert.Equal("Hello World", retValue);
        }

        [Fact]
        public void GenericListWithCustomizedType()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("using Feval.UnitTests");
            Eval("list = new List<Vector2>()");
            Eval("list.Add(new Vector2(1, 1))");
            Eval("list[0]");
            Assert.Equal(retValue, new Vector2(1, 1));
        }

        [Fact]
        public void GenericDictionaryWithBaseTypes()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("dict = new Dictionary<int, string>()");
            Eval("dict.Add(1, \"Hello Dictionary\")");
            Eval("dict[1]");
            Assert.Equal("Hello Dictionary", retValue);
        }
    }
}