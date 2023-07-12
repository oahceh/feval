using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class StringInterpolationExpressionTests : TestCase
    {
        public StringInterpolationExpressionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            Eval("a = \"Hello\"");
            Eval("b = \"World\"");
            Eval("$\"{a} {b}\"");
            Assert.Equal("Hello World", retValue);
            
            Eval("$\"Hello {b.ToUpper()}\"");
            Assert.Equal("Hello WORLD", retValue);
        }
    }
}