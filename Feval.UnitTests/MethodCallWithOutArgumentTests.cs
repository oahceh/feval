using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class MethodCallWithOutArgument
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static Dictionary<int, string> map = new Dictionary<int, string>
        {
            { 1, "One" },
            { 2, "Two" },
            { 3, "Four" }
        };

        public static bool TryGet(string a, out string b, string c = "")
        {
            b = $"Hello {a}";
            return a == "World";
        }
    }

    public class MethodCallWithOutArgumentTests : TestCase
    {
        public MethodCallWithOutArgumentTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            Eval("using Feval.UnitTests");
            WriteLine();

            Eval("MethodCallWithOutArgument.TryGet(\"World\", out v1)");
            Assert.Equal(true, retValue);
            Eval("v1");
            Assert.Equal("Hello World", retValue);
            WriteLine();

            Eval("MethodCallWithOutArgument.map.TryGetValue(2, out v2)");
            Assert.Equal(true, retValue);
            Eval("v2");
            Assert.Equal("Two", retValue);
            WriteLine();

            Eval("MethodCallWithOutArgument.map.TryGetValue(4, out v3)");
            Assert.Equal(false, retValue);
            Eval("v3");
            Assert.Null(retValue);
        }
    }
}