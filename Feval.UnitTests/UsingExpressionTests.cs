using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class System
    {
        public static string Get()
        {
            return nameof(UnitTests);
        }
    }

    public class UsingTest
    {
        public UsingTest(int a, int b)
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

    public class UsingExpressionTests : TestCase
    {
        public UsingExpressionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            Eval("using Feval.UnitTests");
            Eval("var instance = new UsingTest(1, 2)");
            Eval("instance.Add()");
            Assert.Equal(3, retValue);

            WriteLine();
            Eval("System.Get()");
            Assert.Equal(nameof(UnitTests), retValue);
        }
    }
}