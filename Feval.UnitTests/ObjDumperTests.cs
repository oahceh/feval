using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class ObjDumperTests : TestCase
    {
        public ObjDumperTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            context.RegisterDumper(obj => $"ObjDump: {obj}");
            Eval("dump(\"hello world\")");
            Assert.Equal("ObjDump: hello world", retValue.ToString());
        }
    }
}