using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Feval
{
    internal static class ReflectionUtilities
    {
        #region Interface

        /// <summary>
        /// 通过反射获取指定对象的属性或者字段的值
        /// </summary>
        /// <param name="instance">指定对象实例</param>
        /// <param name="name">属性或者字段名</param>
        public static object GetValue(object instance, string name)
        {
            instance.GetType().TryGetMemberValue(name, out var ret, instance);
            return ret;
        }

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

        public static string FormatGenericTypeName(string name, Type[] genericArgumentTypes)
        {
            var builder = new StringBuilder();
            builder.Append(name);
            builder.Append('`');
            builder.Append(genericArgumentTypes.Length);
            builder.Append('[');
            for (var i = 0; i < genericArgumentTypes.Length; i++)
            {
                builder.Append(genericArgumentTypes[i].FullName);
                if (i != genericArgumentTypes.Length - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(']');

            return builder.ToString();
        }

        #endregion
    }
}