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
        public void BitwiseOr()
        {
            Eval("using System.Reflection");
            Eval("BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy");
            Assert.Equal(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy, retValue);
        }

        [Fact]
        public void Add()
        {
            WriteLine("Add two integers:");
            Eval("1 + 2");
            Assert.Equal(1 + 2, retValue);

            Separator();

            WriteLine("Add float and int:");
            Eval("1.0 + 2");
            Assert.Equal(1.0f + 2, retValue);

            Separator();

            WriteLine("Add custom types:");
            Eval("using Feval.UnitTests");
            Eval("new Vector2(1, 2) + new Vector2(3, 4)");
            Assert.Equal(new Vector2(1, 2) + new Vector2(3, 4), retValue);

            Separator();

            WriteLine("Add custom type and int:");
            Eval("new Vector2(1, 2) + 3");
            Assert.Equal(new Vector2(1, 2) + 3, retValue);
        }

        [Fact]
        public void Subtraction()
        {
            WriteLine("Subtraction between two integers:");
            Eval("2 - 1");
            Assert.Equal(2 - 1, retValue);

            Separator();

            WriteLine("Subtraction between float and int:");
            Eval("2.2 - 1");
            Assert.Equal(2.2f - 1, retValue);

            Separator();

            WriteLine("Subtraction between custom types:");
            Eval("using Feval.UnitTests");
            Eval("new Vector2(1, 2) - new Vector2(3, 4)");
            Assert.Equal(new Vector2(1, 2) - new Vector2(3, 4), retValue);

            Separator();

            WriteLine("Subtraction between custom type and int:");
            Eval("using Feval.UnitTests");
            Eval("new Vector2(1, 2) - 3");
            Assert.Equal(new Vector2(1, 2) - 3, retValue);
        }

        [Fact]
        public void Multiply()
        {
            WriteLine("Multiply two integers:");
            Eval("2 * 3");
            Assert.Equal(2 * 3, retValue);

            Separator();

            WriteLine("Multiply float and int:");
            Eval("2.2 * 3");
            Assert.Equal(2.2f * 3, retValue);

            Separator();

            WriteLine("Multiply custom type and int:");
            Eval("using Feval.UnitTests");
            Eval("new Vector2(1, 2) * 3");
            Assert.Equal(new Vector2(1, 2) * 3, retValue);
        }

        [Fact]
        public void Division()
        {
            WriteLine("Divide two integers:");
            Eval("4 / 2");
            Assert.Equal(4 / 2, retValue);

            Separator();

            WriteLine("Divide float and int:");
            Eval("4.4 / 2");
            Assert.Equal(4.4f / 2, retValue);

            Separator();

            WriteLine("Divide custom type and int:");
            Eval("using Feval.UnitTests");
            Eval("new Vector2(4, 6) / 2");
            Assert.Equal(new Vector2(4, 6) / 2, retValue);
        }
    }
}