using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Feval
{
    internal static class ExtensionMethodCache
    {
        public static MethodInfo FindExtensionMethod(Type targetType, string methodName, Type[] parameterTypes)
        {
            var key =
                $"{targetType.FullName}.{methodName}({string.Join(",", parameterTypes.Select(t => t.FullName))})";

            if (m_Cache.TryGetValue(key, out var method))
                return method;

            method = ExtensionMethodFinder.FindExtensionMethod(targetType, methodName, parameterTypes);
            if (method != null)
            {
                m_Cache.TryAdd(key, method);
            }

            return method;
        }

        public static MethodInfo FindExtensionMethods(string methodName, Type targetType, Type[] genericArguments,
            Type[] argumentTypes)
        {
            // 生成缓存键
            var key =
                $"{methodName}_{targetType.FullName}_{string.Join(",", genericArguments.Select(t => t.FullName))}_{string.Join(",", argumentTypes.Select(t => t.FullName))}";

            if (m_Cache.TryGetValue(key, out var method))
            {
                return method;
            }

            method = ExtensionMethodFinder.FindExtensionMethod(methodName, targetType, genericArguments, argumentTypes);
            if (method != null)
            {
                m_Cache.TryAdd(key, method);
            }

            return method;
        }

        private static readonly ConcurrentDictionary<string, MethodInfo> m_Cache = new();
    }
}