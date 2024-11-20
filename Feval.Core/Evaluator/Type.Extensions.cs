using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Feval
{
    internal static class TypeExtensions
    {
        #region Interface

        public static void GetPropertyOrField(this Type type, string name, out PropertyInfo propertyInfo,
            out FieldInfo fieldInfo, object instance = null)
        {
            while (true)
            {
                // 注: 静态成员的继承需要 BindingFlags.FlattenHierarchy
                var flag = instance != null ? BindingFlags.Instance : BindingFlags.Static;
                var flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | flag;
                fieldInfo = Reflector.GetField(type, name, flags);
                propertyInfo = Reflector.GetProperty(type, name, flags);

                // Looking upwards
                if (instance != null && fieldInfo == null && propertyInfo == null && type.BaseType != null &&
                    type.BaseType != typeof(object))
                {
                    type = type.BaseType;
                    continue;
                }

                break;
            }
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

        public static string FormatMethodName(string name, object[] args)
        {
            var types = GetTypes(args);
            var builder = new StringBuilder();
            builder.Append(name);
            builder.Append("(");
            for (var i = 0; i < types.Length; i++)
            {
                builder.Append(types[i]?.Name ?? Type.Missing);
                if (i != types.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(")");
            return builder.ToString();
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags flags, object[] arguments)
        {
            return type.GetMethods(name, flags, GetTypes(arguments), false).FirstOrDefault();
        }

        public static MethodInfo GetExtensionMethod(this Type type, string name, object[] arguments)
        {
            return ExtensionMethodCache.FindExtensionMethod(type, name, GetTypes(arguments));
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

        public static Type[] OfTypes(this object[] args)
        {
            return GetTypes(args);
        }

        #endregion

        #region Method

        private static Type[] GetTypes(object[] args)
        {
            var types = new Type[args.Length];
            for (var i = 0; i < types.Length; i++)
            {
                types[i] = args[i]?.GetType();
            }

            return types;
        }

        private static MethodInfo[] GetMethods(this Type type, string name, BindingFlags flags, Type[] argTypes,
            bool generic)
        {
            return Reflector.GetMethods(type, name, flags).Where(method =>
                    generic == method.ContainsGenericParameters && MatchArgumentAndParameterTypes(method, argTypes))
                .ToArray();
        }

        private static bool CanChangeType(Type argType, Type paraType)
        {
            if (argType == null)
            {
                return true;
            }

            return argType == paraType || argType.IsSubclassOf(paraType) || paraType.IsAssignableFrom(argType);
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
                if (!CanChangeType(argTypes[i], parameter.ParameterType) &&
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