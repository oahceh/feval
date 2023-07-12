using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class TypeKeywordsTests : TestCase
    {
        public TypeKeywordsTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void StringKeywordTest()
        {
            Eval("string.IsNullOrEmpty(\"Hello World\")");
            Assert.False((bool) retValue);
        }

        [Fact]
        public void IntKeywordTest()
        {
            Eval("int.MaxValue");
            Assert.Equal(int.MaxValue, retValue);
        }

        [Fact]
        public void LongKeywordTest()
        {
            Eval("long.MaxValue");
            Assert.Equal(long.MaxValue, retValue);
        }

        [Fact]
        public void FloatKeywordTest()
        {
            Eval("float.MaxValue");
            Assert.Equal(float.MaxValue, retValue);
        }

        [Fact]
        public void DoubleKeywordTest()
        {
            Eval("double.MaxValue");
            Assert.Equal(double.MaxValue, retValue);
        }
    }
}