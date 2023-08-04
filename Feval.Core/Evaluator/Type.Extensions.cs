using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Feval
{
    internal static class TypeExtensions
    {
        #region Interface

        public static void GetPropertyOrField(this Type type, string name, out PropertyInfo propertyInfo,
            out FieldInfo fieldInfo, object instance = null)
        {
            // 注: 静态成员的继承需要 BindingFlags.FlattenHierarchy
            var flag = instance != null ? BindingFlags.Instance : BindingFlags.Static | BindingFlags.FlattenHierarchy;
            var flags = BindingFlags.Public | BindingFlags.NonPublic | flag;
            fieldInfo = type.GetField(name, flags);
            propertyInfo = type.GetProperty(name, flags);
        }

        public static MethodInfo GetGenericMethod(this Type t, string name, BindingFlags flags, Type[] genericArgTypes,
            Type[] argTypes,
            Type returnType)
        {
            var methods = t.GetMethods(name, flags, argTypes, true);
            foreach (var method in methods)
            {
                var genericArguments = method.GetGenericArguments();
                if (genericArguments.Length == genericArgTypes.Length)
                {
                    return method.MakeGenericMethod(genericArgTypes);
                }
            }

            return null;
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags flags, object[] arguments)
        {
            var types = new Type[arguments.Length];
            for (var i = 0; i < types.Length; i++)
            {
                types[i] = arguments[i]?.GetType();
            }

            return type.GetMethods(name, flags, types, false).FirstOrDefault();
        }

        public static object Invoke(this MethodInfo method, object obj, ref object[] arguments,
            bool completingDefaultArgs)
        {
            var parameters = method.GetParameters();
            if (completingDefaultArgs && parameters.Length != arguments.Length)
            {
                var defaultArgs = parameters.Skip(arguments.Length)
                    .Select(a => a.HasDefaultValue ? a.DefaultValue : null);
                arguments = arguments.Concat(defaultArgs).ToArray();
            }

            return method.Invoke(obj, arguments);
        }

        public static List<MethodInfo> GetMethods(this Type type, string name, BindingFlags flags)
        {
            var methods = type.GetMethods(flags);
            return methods.Where(method => method.Name == name).ToList();
        }

        public static MethodInfo[] GetMethods(this Type type, string name, BindingFlags flags, Type[] argTypes,
            bool generic)
        {
            var methods = type.GetMethods(name, flags);
            return methods.Where(method =>
                    generic == method.ContainsGenericParameters && MatchArgumentAndParameterTypes(method, argTypes))
                .ToArray();
        }

        #endregion

        #region Method

        private static bool Convertable(Type argType, Type paraType)
        {
            return argType == null || argType == paraType || argType.IsSubclassOf(paraType);
        }

        /// <summary>
        /// 匹配指定方法形参实参类型
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <param name="argTypes">实参类型数组</param>
        /// <returns>是否匹配</returns>
        private static bool MatchArgumentAndParameterTypes(MethodInfo methodInfo, Type[] argTypes)
        {
            var parameters = methodInfo.GetParameters();
            var argCount = argTypes?.Length ?? 0;
            var paraCount = parameters.Length;

            // 实参只可能比形参少
            if (argCount > paraCount)
            {
                return false;
            }

            for (var i = 0; i < argCount; i++)
            {
                var parameter = parameters[i];
                if (!Convertable(argTypes[i], parameter.ParameterType) &&
                    !parameter.ParameterType.IsGenericParameter &&
                    !parameter.IsOut)
                {
                    return false;
                }
            }

            // 剩下的只有可能是默认参数
            var count = paraCount - argCount;
            for (var i = 0; i < count; i++)
            {
                var parameter = parameters[i + argCount];
                if (!parameter.HasDefaultValue)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}