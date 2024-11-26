using System;
using System.Linq;
using System.Reflection;

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

        public static object OperatorAdd(object a, object b)
        {
            ThrowIfArgumentIsNull(a, b);

            // 处理基础数值类型相加
            if (IsNumericType(a.GetType()) && IsNumericType(b.GetType()))
            {
                return AddNumeric(a, b);
            }

            return Operator("op_Addition", a, b);
        }

        public static object OperatorSubtraction(object a, object b)
        {
            ThrowIfArgumentIsNull(a, b);
            if (IsNumericType(a.GetType()) && IsNumericType(b.GetType()))
            {
                return SubtractionNumeric(a, b);
            }

            return Operator("op_Subtraction", a, b);
        }

        public static object OperatorMultiply(object a, object b)
        {
            ThrowIfArgumentIsNull(a, b);
            if (IsNumericType(a.GetType()) && IsNumericType(b.GetType()))
            {
                return MultiplyNumeric(a, b);
            }

            return Operator("op_Multiply", a, b);
        }

        public static object OperatorDivision(object a, object b)
        {
            ThrowIfArgumentIsNull(a, b);
            if (IsNumericType(a.GetType()) && IsNumericType(b.GetType()))
            {
                return DivisionNumeric(a, b);
            }

            return Operator("op_Division", a, b);
        }

        #endregion

        #region Methods

        private static void ThrowIfArgumentIsNull(object a, object b)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }
        }

        private static bool IsNumericType(Type type)
        {
            return m_NumericTypes.Contains(type);
        }

        private static object SubtractionNumeric(object a, object b)
        {
            var targetType = GetHigherPriorityType(a.GetType(), b.GetType());
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Byte:
                    return Convert.ToByte(a) - Convert.ToByte(b);
                case TypeCode.SByte:
                    return Convert.ToSByte(a) - Convert.ToSByte(b);
                case TypeCode.Int16:
                    return Convert.ToInt16(a) - Convert.ToInt16(b);
                case TypeCode.UInt16:
                    return Convert.ToUInt16(a) - Convert.ToUInt16(b);
                case TypeCode.Int32:
                    return Convert.ToInt32(a) - Convert.ToInt32(b);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(a) - Convert.ToUInt32(b);
                case TypeCode.Int64:
                    return Convert.ToInt64(a) - Convert.ToInt64(b);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(a) - Convert.ToUInt64(b);
                case TypeCode.Single:
                    return Convert.ToSingle(a) - Convert.ToSingle(b);
                case TypeCode.Double:
                    return Convert.ToDouble(a) - Convert.ToDouble(b);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(a) - Convert.ToDecimal(b);
                default:
                    throw new InvalidOperationException("Unsupported numeric type.");
            }
        }

        private static object MultiplyNumeric(object a, object b)
        {
            var targetType = GetHigherPriorityType(a.GetType(), b.GetType());
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Byte:
                    return Convert.ToByte(a) * Convert.ToByte(b);
                case TypeCode.SByte:
                    return Convert.ToSByte(a) * Convert.ToSByte(b);
                case TypeCode.Int16:
                    return Convert.ToInt16(a) * Convert.ToInt16(b);
                case TypeCode.UInt16:
                    return Convert.ToUInt16(a) * Convert.ToUInt16(b);
                case TypeCode.Int32:
                    return Convert.ToInt32(a) * Convert.ToInt32(b);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(a) * Convert.ToUInt32(b);
                case TypeCode.Int64:
                    return Convert.ToInt64(a) * Convert.ToInt64(b);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(a) * Convert.ToUInt64(b);
                case TypeCode.Single:
                    return Convert.ToSingle(a) * Convert.ToSingle(b);
                case TypeCode.Double:
                    return Convert.ToDouble(a) * Convert.ToDouble(b);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(a) * Convert.ToDecimal(b);
                default:
                    throw new InvalidOperationException("Unsupported numeric type.");
            }
        }

        private static object DivisionNumeric(object a, object b)
        {
            var targetType = GetHigherPriorityType(a.GetType(), b.GetType());
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Byte:
                    return Convert.ToByte(a) / Convert.ToByte(b);
                case TypeCode.SByte:
                    return Convert.ToSByte(a) / Convert.ToSByte(b);
                case TypeCode.Int16:
                    return Convert.ToInt16(a) / Convert.ToInt16(b);
                case TypeCode.UInt16:
                    return Convert.ToUInt16(a) / Convert.ToUInt16(b);
                case TypeCode.Int32:
                    return Convert.ToInt32(a) / Convert.ToInt32(b);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(a) / Convert.ToUInt32(b);
                case TypeCode.Int64:
                    return Convert.ToInt64(a) / Convert.ToInt64(b);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(a) / Convert.ToUInt64(b);
                case TypeCode.Single:
                    return Convert.ToSingle(a) / Convert.ToSingle(b);
                case TypeCode.Double:
                    return Convert.ToDouble(a) / Convert.ToDouble(b);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(a) / Convert.ToDecimal(b);
                default:
                    throw new InvalidOperationException("Unsupported numeric type.");
            }
        }

        private static object AddNumeric(object a, object b)
        {
            var targetType = GetHigherPriorityType(a.GetType(), b.GetType());
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Byte:
                    return Convert.ToByte(a) + Convert.ToByte(b);
                case TypeCode.SByte:
                    return Convert.ToSByte(a) + Convert.ToSByte(b);
                case TypeCode.Int16:
                    return Convert.ToInt16(a) + Convert.ToInt16(b);
                case TypeCode.UInt16:
                    return Convert.ToUInt16(a) + Convert.ToUInt16(b);
                case TypeCode.Int32:
                    return Convert.ToInt32(a) + Convert.ToInt32(b);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(a) + Convert.ToUInt32(b);
                case TypeCode.Int64:
                    return Convert.ToInt64(a) + Convert.ToInt64(b);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(a) + Convert.ToUInt64(b);
                case TypeCode.Single:
                    return Convert.ToSingle(a) + Convert.ToSingle(b);
                case TypeCode.Double:
                    return Convert.ToDouble(a) + Convert.ToDouble(b);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(a) + Convert.ToDecimal(b);
                default:
                    throw new InvalidOperationException("Unsupported numeric type.");
            }
        }

        private static Type GetHigherPriorityType(Type typeA, Type typeB)
        {
            var indexA = Array.IndexOf(m_NumericTypes, typeA);
            var indexB = Array.IndexOf(m_NumericTypes, typeB);

            if (indexA == -1 || indexB == -1)
            {
                throw new ArgumentException("Unsupported numeric type.");
            }

            return indexA > indexB ? typeA : typeB;
        }

        private static object Operator(string operatorName, object a, object b)
        {
            var operatorMethod = a.GetType().GetMethod(operatorName, BindingFlags.Public | BindingFlags.Static, null,
                new[] { a.GetType(), b.GetType() }, null);

            if (operatorMethod != null)
            {
                return operatorMethod.Invoke(null, new[] { a, b });
            }

            throw new InvalidOperationException(
                $"The type {a.GetType().FullName} does not overload the '{operatorName}' operator.");
        }

        #endregion

        #region Fields

        private static readonly Type[] m_NumericTypes =
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        #endregion
    }
}