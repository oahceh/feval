using System;
using System.Reflection;
using Feval.Syntax;

namespace Feval
{
    internal static class ReflectionUtilities
    {
        #region Interface

        public static bool TryGetMemberValue(object instance, string memberName, out object value)
        {
            return instance.GetType().TryGetMemberValue(memberName, out value, instance);
        }

        /// <summary>
        /// 通过反射获取属性或者字段的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">属性或者字段名</param>
        /// <param name="value">要设置的值</param>
        /// <param name="instance">指定实例，若不为空则为实例属性或字段否则为静态属性或字段</param>
        public static bool TryGetMemberValue(this Type type, string name, out object value, object instance = null)
        {
            type.GetPropertyOrField(name, out var propertyInfo, out var fieldInfo, instance);
            if (fieldInfo != null)
            {
                value = fieldInfo.GetValue(instance);
                return true;
            }

            if (propertyInfo != null)
            {
                value = propertyInfo.GetValue(instance);
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 通过反射设置指定对象的属性或者字段的值
        /// </summary>
        /// <param name="instance">指定对象实例</param>
        /// <param name="name">属性或者字段名</param>
        /// <param name="value">要设置的值</param>
        public static void SetValue(object instance, string name, object value)
        {
            instance.GetType().SetValue(name, value, instance);
        }

        /// <summary>
        /// 通过反射设置属性或者字段的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">属性或者字段名</param>
        /// <param name="value">要设置的值</param>
        /// <param name="instance">指定实例，若不为空则为实例属性或字段否则为静态属性或字段</param>
        public static void SetValue(this Type type, string name, object value, object instance = null)
        {
            type.GetPropertyOrField(name, out var propertyInfo, out var fieldInfo, instance);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(instance, value);
            }
            else if (fieldInfo != null)
            {
                fieldInfo.SetValue(instance, value);
            }
        }

        public static object BitwiseOr(object left, object right)
        {
            var leftType = left.GetType();
            var rightType = right.GetType();
            if (leftType != rightType)
            {
                throw new Exception($"Cannot apply bitwise or operation on different types {leftType} and {rightType}");
            }

            var isEnum = leftType.IsEnum;
            var type = isEnum ? Enum.GetUnderlyingType(leftType) : leftType;

            if (type.IsPrimitive)
            {
                if (type == typeof(int))
                {
                    var value = (int) left | (int) right;
                    return isEnum ? Enum.ToObject(leftType, value) : value;
                }

                if (type == typeof(long))
                {
                    var value = (long) left | (long) right;
                    return isEnum ? Enum.ToObject(leftType, value) : value;
                }

                if (type == typeof(byte))
                {
                    var value = (byte) left | (byte) right;
                    return isEnum ? Enum.ToObject(leftType, value) : value;
                }

                throw new Exception($"Operator or not supported for type {leftType}");
            }

            var orMethod = leftType.GetMethod("op_BitwiseOr", BindingFlags.Public | BindingFlags.Static);
            if (orMethod == null)
            {
                throw new Exception($"Cannot find bitwise or operator for type {leftType}");
            }

            return orMethod.Invoke(null, new[] { left, right });
        }

        #endregion
    }
}