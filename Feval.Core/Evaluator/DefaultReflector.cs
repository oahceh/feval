using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Feval
{
    internal sealed class DefaultReflector : IReflector
    {
        public Type GetType(Assembly assembly, string name)
        {
            return assembly.GetType(name);
        }

        public FieldInfo GetField(Type type, string name, BindingFlags flags)
        {
            return type.GetField(name, flags);
        }

        public PropertyInfo GetProperty(Type type, string name, BindingFlags flags)
        {
            return type.GetProperty(name, flags);
        }

        public IEnumerable<MethodInfo> GetMethods(Type type, string name, BindingFlags flags)
        {
            var methods = type.GetMethods(flags);
            return methods.Where(method => method.Name == name);
        }
    }
}