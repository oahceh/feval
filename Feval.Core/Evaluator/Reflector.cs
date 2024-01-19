using System;
using System.Collections.Generic;
using System.Reflection;

namespace Feval
{
    internal static class Reflector
    {
        public static Type GetType(Assembly assembly, string name)
        {
            return Instance.GetType(assembly, name);
        }

        public static FieldInfo GetField(Type type, string name, BindingFlags flags)
        {
            return Instance.GetField(type, name, flags);
        }

        public static PropertyInfo GetProperty(Type type, string name, BindingFlags flags)
        {
            return Instance.GetProperty(type, name, flags);
        }

        public static IEnumerable<MethodInfo> GetMethods(Type type, string name, BindingFlags flags)
        {
            return Instance.GetMethods(type, name, flags);
        }

        internal static IReflector Instance { get; set; } = new DefaultReflector();
    }
}