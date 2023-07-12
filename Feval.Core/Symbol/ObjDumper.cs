using System;

namespace Feval
{
    internal static class ObjDumper
    {
        public static Func<object, string> Dumper { get; set; } = DefaultDump;

        public static string Dump(object obj)
        {
            return Dumper.Invoke(obj);
        }

        private static string DefaultDump(object obj)
        {
            return obj?.ToString() ?? "null";
        }
    }
}