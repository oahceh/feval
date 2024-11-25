using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class BinaryExpressionTests : TestCase
    {
        public BinaryExpressionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void BitwiseOrOperator()
        {
            Eval("using System.Reflection");
            Eval("BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy");
            Assert.Equal(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy, retValue);
        }
    }
}