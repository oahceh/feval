using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class TypeOfExpressionTests : TestCase
    {
        public TypeOfExpressionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CustomizedTypesTest()
        {
            Eval("using Feval.UnitTests");
            Eval("typeof(TypeOfExpressionTests)");
            Assert.Equal(typeof(TypeOfExpressionTests), retValue);
        }

        [Fact]
        public void PrimitiveTypesTests()
        {
            Eval("typeof(string)");
            Assert.Equal(typeof(string), retValue);

            Eval("typeof(int)");
            Assert.Equal(typeof(int), retValue);

            Eval("typeof(long)");
            Assert.Equal(typeof(long), retValue);

            Eval("typeof(float)");
            Assert.Equal(typeof(float), retValue);

            Eval("typeof(double)");
            Assert.Equal(typeof(double), retValue);
        }
    }
}