using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class Parent
    {
    }

    public class Child : Parent
    {
        public static Child[] children = { new Child() };
    }

    public class StaticMethodCall
    {
        public static string PrintObject(object o)
        {
            return o.GetType().Name;
        }

        public static string Print(Parent p)
        {
            return p.GetType().Name;
        }

        public static string CallWithArray(Parent[] ps)
        {
            return ps.GetType().Name;
        }

        public static string CallWithIEnumerable(IEnumerable<Parent> ps)
        {
            return ps.GetType().Name;
        }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static float Add(float a, int b)
        {
            return a + b;
        }

        public static string Get(string a = "Hello World")
        {
            return a;
        }

        public static void Print()
        {
            Console.WriteLine("Hello World");
        }
    }

    public class StaticMethodCallTests : TestCase
    {
        public StaticMethodCallTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CallMethodWithDerivedArgType()
        {
            Eval("using Feval.UnitTests");
            Eval("StaticMethodCall.Print(new Child())");
            Assert.Equal(nameof(Child), retValue);

            Eval("StaticMethodCall.PrintObject(new Child())");
            Assert.Equal(nameof(Child), retValue);

            Eval("StaticMethodCall.CallWithArray(Child.children)");
            Assert.Equal(Child.children.GetType().Name, retValue);

            Eval("StaticMethodCall.CallWithIEnumerable(Child.children)");
            Assert.Equal(Child.children.GetType().Name, retValue);
        }

        [Fact]
        public void Test()
        {
            Eval("using Feval.UnitTests");
            Assert.False(WithReturn);
            WriteLine();

            Eval("StaticMethodCall.Print()");
            Assert.False(WithReturn);
            WriteLine();

            Eval("StaticMethodCall.Add(1, 2)");
            Assert.Equal(3, retValue);
            WriteLine();

            Eval("StaticMethodCall.Add(1, -1)");
            Assert.Equal(0, retValue);
            WriteLine();

            Eval("StaticMethodCall.Add(-0.1, 1)");
            Assert.Equal(0.9f, retValue);
            WriteLine();

            Eval("StaticMethodCall.Get()");
            Assert.Equal("Hello World", retValue);
        }
    }
}