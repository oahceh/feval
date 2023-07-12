using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class GenericMember
    {
        public int Value { get; }

        public GenericMember(int value)
        {
            Value = value;
        }
    }

    public class GenericAdd
    {
        public static string Get<T>()
        {
            return typeof(T).Name;
        }

        public static string Get()
        {
            return "Hello World";
        }

        public string Add<T>(T a, T b) where T : GenericMember
        {
            return $"{a.Value} {b.Value}";
        }

        public static string AddStatic<T>(T a, T b) where T : GenericMember
        {
            return $"{a.Value} {b.Value}";
        }
    }

    public class GenericMethodCallTests : TestCase
    {
        public GenericMethodCallTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void MethodWithTypeKeyword()
        {
            Eval("using Feval.UnitTests");
            Eval("GenericAdd.Get<string>()");
            Assert.Equal("String", retValue);
        }

        [Fact]
        public void OverrideMethodCall()
        {
            Eval("using Feval.UnitTests");
            Eval("GenericAdd.Get()");
            Assert.Equal("Hello World", retValue);
        }

        [Fact]
        public void InstanceMethodAdd()
        {
            Eval("using Feval.UnitTests");
            Eval("var instance = new GenericAdd()");
            Eval("var m1 = new GenericMember(1)");
            Eval("var m2 = new GenericMember(2)");
            Eval("instance.Add<GenericMember>(m1, m2)");
            Assert.Equal("1 2", retValue);
        }

        [Fact]
        public void StaticMethodAdd()
        {
            Eval("using Feval.UnitTests");
            Eval("var instance = new GenericAdd()");
            Eval("var m1 = new GenericMember(1)");
            Eval("var m3 = new GenericMember(3)");
            Eval("GenericAdd.AddStatic<GenericMember>(m1, m3)");
            Assert.Equal("1 3", retValue);
        }
    }
}