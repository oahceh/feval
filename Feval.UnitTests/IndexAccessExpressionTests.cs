using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class IndexAccessTestCase
    {
        public static List<string> list = new List<string>
        {
            "Hello", "World"
        };

        public static string[] array = new[]
        {
            "Hello", "World"
        };

        public static Dictionary<string, string> dictionary = new Dictionary<string, string>
        {
            { "Hello", "World" }
        };
    }

    public class IndexAccessExpressionTests : TestCase
    {
        public IndexAccessExpressionTests(ITestOutputHelper output) : base(output)
        {
            Eval("using Feval.UnitTests");
        }

        [Fact]
        public void ListIndexAccess()
        {
            Eval("IndexAccessTestCase.list[0]");
            Assert.Equal("Hello", retValue);
            Eval("IndexAccessTestCase.list[1]");
            Assert.Equal("World", retValue);
        }

        [Fact]
        public void ArrayIndexAccess()
        {
            Eval("IndexAccessTestCase.array[0]");
            Assert.Equal("Hello", retValue);
            Eval("IndexAccessTestCase.array[1]");
            Assert.Equal("World", retValue);
        }

        [Fact]
        public void DictionaryIndexAccess()
        {
            Eval("IndexAccessTestCase.dictionary[\"Hello\"]");
            Assert.Equal("World", retValue);
        }
    }
}