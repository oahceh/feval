using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    public class StaticMethodCall
    {
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