using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class AddTest
    {
        public AddTest(int a, int b)
        {
            m_A = a;
            m_B = b;
        }

        public int Add()
        {
            return Add(m_A, m_B);
        }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        private readonly int m_A;

        private readonly int m_B;
    }

    public sealed class ConstructExpressionTests : TestCase
    {
        public ConstructExpressionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void Test()
        {
            Eval("var instance = new Feval.UnitTests.AddTest(1, 2)");
            Eval("instance.Add()");
            Assert.Equal(3, retValue);
        }

        [Fact]
        public void GenericCtorTest()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("list = new List<String>()");
            Eval("list.Add(\"Hello World\")");
            Eval("list[0]");
            Assert.Equal("Hello World", retValue);


            Eval("dict = new Dictionary<Int32, String>()");
            Eval("dict.Add(1, \"Hello Dictionary\")");
            Eval("dict[1]");
            Assert.Equal("Hello Dictionary", retValue);
        }
    }
}