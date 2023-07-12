using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Feval.UnitTests
{
    internal static class TestOutputHelperExtensions
    {
        internal static void WriteInput(this ITestOutputHelper helper, string text)
        {
            text = text ?? string.Empty;
            helper.WriteLine($"> {text}");
        }

        internal static void WriteInput(this ITestOutputHelper helper, List<string> input)
        {
            if (input == null)
            {
                return;
            }

            foreach (var text in input)
            {
                helper.WriteInput(text);
            }
        }
    }
}