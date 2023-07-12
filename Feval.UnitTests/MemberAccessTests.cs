using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class MemberAccessTestClass
    {
        public static string stringValue = "HelloWorld";

        public static object staticObjectValue;

        public static int intValue = 1;

        public object objectValue;

        public void SetValue(int value)
        {
            m_IntValue = value;
        }

        private int m_IntValue;

        private string _stringValue = "HelloWorld";
    }

    public class MemberAccessTests : TestCase
    {
        public MemberAccessTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Test()
        {
            Eval("using Feval.UnitTests");

            // Static member access
            Eval("MemberAccessTestClass.stringValue");
            Assert.Equal("HelloWorld", retValue);
            WriteLine();

            Eval("MemberAccessTestClass.staticObjectValue");
            Assert.Null(retValue);
            WriteLine();

            // Instance member access
            Eval("var instance = new MemberAccessTestClass()");
            Eval("instance.SetValue(1)");
            Eval("instance.m_IntValue");
            Assert.Equal(1, retValue);

            Eval("instance.objectValue");
            Assert.Null(retValue);

            Eval("instance._stringValue");
            Assert.Equal("HelloWorld", retValue);
        }
    }
}