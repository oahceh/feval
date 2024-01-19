using System;
using System.Collections.Generic;
using System.Reflection;

namespace Feval
{
    public interface IReflector
    {
        Type GetType(Assembly assembly, string name);

        FieldInfo GetField(Type type, string name, BindingFlags flags);

        PropertyInfo GetProperty(Type type, string name, BindingFlags flags);

        IEnumerable<MethodInfo> GetMethods(Type type, string name, BindingFlags flags);
    }
}