using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public sealed class NestedTypeTests : TestCase
    {
        public NestedTypeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void NestedTypeTest()
        {
            Eval("using Feval.UnitTests");

            Eval("typeof(ParentType.NestedType)");
            Assert.Equal(typeof(ParentType.NestedType), retValue);

            Eval("ParentType.NestedType.Name");
            Assert.Equal(ParentType.NestedType.Name, retValue);
        }
    }
}