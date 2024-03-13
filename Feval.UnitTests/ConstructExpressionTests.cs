using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class Vector2
    {
        public int x;

        public int y;

        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Vector2);
        }

        protected bool Equals(Vector2 other)
        {
            return x == other.x && y == other.y;
        }
    }

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
        public void NonGenericType()
        {
            Eval("var instance = new Feval.UnitTests.AddTest(1, 2)");
            Eval("instance.Add()");
            Assert.Equal(3, retValue);
        }

        [Fact]
        public void GenericListWithBaseType()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("list = new List<string>()");
            Eval("list.Add(\"Hello World\")");
            Eval("list[0]");
            Assert.Equal("Hello World", retValue);
        }

        [Fact]
        public void GenericListWithCustomizedType()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("using Feval.UnitTests");
            Eval("list = new List<Vector2>()");
            Eval("list.Add(new Vector2(1, 1))");
            Eval("list[0]");
            Assert.Equal(retValue, new Vector2(1, 1));
        }

        [Fact]
        public void GenericDictionaryWithBaseTypes()
        {
            Eval("using System");
            Eval("using System.Collections.Generic");
            Eval("dict = new Dictionary<int, string>()");
            Eval("dict.Add(1, \"Hello Dictionary\")");
            Eval("dict[1]");
            Assert.Equal("Hello Dictionary", retValue);
        }
    }
}