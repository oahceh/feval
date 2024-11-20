using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Feval
{
    internal static class ExtensionMethodFinder
    {
        /// <summary>
        /// 查找所有适用于指定类型的扩展方法。
        /// </summary>
        /// <param name="targetType">目标类型，即你要扩展的方法的接收者类型。</param>
        /// <returns>返回适用于目标类型的扩展方法集合。</returns>
        public static IEnumerable<MethodInfo> GetExtensionMethods(Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            // 获取所有加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // 跳过动态生成的程序集以避免反射异常
                if (assembly.IsDynamic)
                    continue;

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    // 扩展方法必须定义在静态类中
                    if (!type.IsSealed || !type.IsAbstract) // 静态类在 C# 中是抽象且密封的
                        continue;

                    // 查找所有静态方法
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        // 检查方法是否具有 [Extension] 特性
                        if (!method.IsDefined(typeof(ExtensionAttribute), false))
                            continue;

                        var parameters = method.GetParameters();
                        if (parameters.Length == 0)
                            continue;

                        // 第一个参数类型应与 targetType 或其基类/接口匹配
                        var extendedType = parameters[0].ParameterType;

                        if (IsAssignableFrom(extendedType, targetType))
                        {
                            yield return method;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查一个类型是否可以赋值给另一个类型，考虑泛型和接口。
        /// </summary>
        private static bool IsAssignableFrom(Type extendedType, Type targetType)
        {
            if (extendedType.IsGenericParameter)
                return true; // 简化处理，视具体需求调整

            return extendedType.IsAssignableFrom(targetType);
        }

        /// <summary>
        /// 根据方法名称和参数类型查找扩展方法。
        /// </summary>
        public static MethodInfo FindExtensionMethod(Type targetType, string methodName, Type[] parameterTypes)
        {
            var methods = GetExtensionMethods(targetType)
                .Where(m => m.Name == methodName)
                .Where(m => {
                    var parameters = m.GetParameters();
                    if (parameters.Length - 1 != parameterTypes.Length)
                        return false;

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (!parameters[i + 1].ParameterType.IsAssignableFrom(parameterTypes[i]))
                            return false;
                    }

                    return true;
                });

            return methods.FirstOrDefault();
        }

        /// <summary>
        /// 查找符合条件的第一个扩展方法，包括基类的扩展方法，对泛型基类有支持
        /// </summary>
        /// <param name="methodName">要查找的扩展方法名称</param>
        /// <param name="targetType">要扩展的目标类型</param>
        /// <param name="genericArguments">泛型参数类型数组</param>
        /// <param name="argumentTypes">扩展方法的实参类型（不包括第一个 this 参数）</param>
        /// <returns>符合条件的第一个 MethodInfo，若无则返回 null</returns>
        public static MethodInfo FindExtensionMethod(string methodName, Type targetType, Type[] genericArguments,
            Type[] argumentTypes)
        {
            // 获取目标类型的继承链，从目标类型本身到 System.Object，包括泛型基类的具体类型
            IEnumerable<Type> inheritanceChain = GetInheritanceChain(targetType);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    // 只处理静态类
                    if (!(type.IsSealed && type.IsAbstract && type.IsClass))
                        continue;

                    // 获取所有静态方法
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        // 检查是否为扩展方法
                        if (!method.IsDefined(typeof(ExtensionAttribute), false))
                            continue;

                        // 检查方法名是否匹配
                        if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                            continue;

                        var parameters = method.GetParameters();

                        // 扩展方法至少有一个参数
                        if (parameters.Length == 0)
                            continue;

                        // 检查第一个参数是否与目标类型或其基类匹配
                        Type extensionType = parameters[0].ParameterType;
                        bool isCompatible = false;

                        foreach (var baseType in inheritanceChain)
                        {
                            if (IsParameterCompatible(extensionType, baseType))
                            {
                                isCompatible = true;
                                break;
                            }
                        }

                        if (!isCompatible)
                            continue;

                        // 检查泛型参数数量是否匹配
                        if (method.IsGenericMethodDefinition)
                        {
                            if (method.GetGenericArguments().Length != genericArguments.Length)
                                continue;
                        }
                        else
                        {
                            if (genericArguments.Length != 0)
                                continue;
                        }

                        // 检查其他参数类型是否匹配
                        if (parameters.Length - 1 != argumentTypes.Length)
                            continue;

                        bool argsMatch = true;
                        for (int i = 0; i < argumentTypes.Length; i++)
                        {
                            var paramType = parameters[i + 1].ParameterType;
                            if (!AreTypesCompatible(paramType, argumentTypes[i]))
                            {
                                argsMatch = false;
                                break;
                            }
                        }

                        if (!argsMatch)
                            continue;

                        // 如果是泛型方法，检查泛型约束是否满足
                        if (method.IsGenericMethodDefinition && genericArguments.Length > 0)
                        {
                            try
                            {
                                var constructedMethod = method.MakeGenericMethod(genericArguments);
                                return constructedMethod;
                            }
                            catch
                            {
                                // 泛型参数不匹配，跳过此方法
                                continue;
                            }

                            /*
                            if (!AreGenericConstraintsSatisfied(constructedMethod))
                            {
                                continue;
                            }
                            */
                        }

                        // 符合所有条件，返回此方法
                        return method;
                    }
                }
            }

            // 未找到符合条件的方法
            return null;
        }

        /// <summary>
        /// 获取目标类型的继承链，从目标类型本身到 System.Object，包括泛型基类的具体类型
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>类型的继承链</returns>
        private static IEnumerable<Type> GetInheritanceChain(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        /// <summary>
        /// 检查方法参数类型是否与提供的类型兼容
        /// 包括处理泛型类型的兼容性
        /// </summary>
        /// <param name="parameterType">方法的参数类型</param>
        /// <param name="providedType">提供的类型</param>
        /// <returns>是否兼容</returns>
        private static bool AreTypesCompatible(Type parameterType, Type providedType)
        {
            // 处理泛型参数
            if (parameterType.IsGenericParameter)
            {
                return true; // 假设所有泛型参数都是兼容的
            }

            // 处理可赋值的类型
            if (parameterType.IsAssignableFrom(providedType))
            {
                return true;
            }

            // 处理泛型类型
            if (parameterType.IsGenericTypeDefinition)
            {
                if (providedType.IsGenericType)
                {
                    var providedGenericDef = providedType.GetGenericTypeDefinition();
                    if (parameterType == providedGenericDef)
                        return true;
                }
            }
            else if (parameterType.IsGenericType && providedType.IsGenericType)
            {
                if (parameterType.GetGenericTypeDefinition() == providedType.GetGenericTypeDefinition())
                {
                    var paramArgs = parameterType.GetGenericArguments();
                    var providedArgs = providedType.GetGenericArguments();

                    if (paramArgs.Length != providedArgs.Length)
                        return false;

                    for (int i = 0; i < paramArgs.Length; i++)
                    {
                        if (!AreTypesCompatible(paramArgs[i], providedArgs[i]))
                            return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 修正后的检查方法参数类型是否与提供的类型兼容，包括泛型类型的深度匹配
        /// </summary>
        /// <param name="parameterType">扩展方法的第一个参数类型</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>是否兼容</returns>
        private static bool IsParameterCompatible(Type parameterType, Type targetType)
        {
            if (parameterType.IsGenericParameter)
                return false;

            if (parameterType.IsAssignableFrom(targetType))
                return true;

            // 处理泛型类型和泛型基类的兼容性
            if (parameterType.IsGenericType)
            {
                if (targetType.IsGenericType)
                {
                    // 比较泛型类型定义
                    if (parameterType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
                    {
                        // 比较泛型参数
                        Type[] paramArgs = parameterType.GetGenericArguments();
                        Type[] targetArgs = targetType.GetGenericArguments();

                        if (paramArgs.Length != targetArgs.Length)
                            return false;

                        for (int i = 0; i < paramArgs.Length; i++)
                        {
                            if (!AreTypesCompatible(paramArgs[i], targetArgs[i]))
                                return false;
                        }

                        return true;
                    }
                }
            }
            else if (parameterType.IsGenericTypeDefinition)
            {
                // 比较泛型类型定义
                if (targetType.IsGenericType && parameterType == targetType.GetGenericTypeDefinition())
                {
                    return true;
                }
            }

            // 递归检查接口实现或基类
            foreach (var iface in targetType.GetInterfaces())
            {
                if (IsParameterCompatible(parameterType, iface))
                    return true;
            }

            if (targetType.BaseType != null)
            {
                if (IsParameterCompatible(parameterType, targetType.BaseType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 检查泛型方法的泛型约束是否满足
        /// </summary>
        /// <param name="method">已构造的泛型方法</param>
        /// <returns>是否满足所有泛型约束</returns>
        private static bool AreGenericConstraintsSatisfied(MethodInfo method)
        {
            var genericArguments = method.GetGenericArguments();

            foreach (var genArg in genericArguments)
            {
                var constraints = genArg.GetGenericParameterConstraints();
                foreach (var constraint in constraints)
                {
                    if (!constraint.IsAssignableFrom(genArg))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}