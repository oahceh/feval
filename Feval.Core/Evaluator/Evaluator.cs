using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Feval.Syntax;

namespace Feval
{
    public readonly struct EvaluationResult
    {
        public bool WithReturn { get; }

        public object Value { get; }

        public EvaluationResult(object value, bool withReturn)
        {
            Value = value;
            WithReturn = withReturn;
        }

        public static EvaluationResult Void => new EvaluationResult(null, false);

        public override string ToString()
        {
            if (Value == null)
            {
                return "null";
            }

            if (Value is string str)
            {
                return $"\"{str}\"";
            }

            return Value.ToString();
        }
    }

    internal sealed class Evaluator
    {
        #region Interface

        internal Evaluator(Context context)
        {
            m_Context = context;
        }

        public EvaluationResult Evaluate(string text, out SyntaxTree tree)
        {
            tree = null;
            if (string.IsNullOrEmpty(text))
            {
                return EvaluationResult.Void;
            }

            tree = SyntaxTree.Parse(text);
            return EvaluateExpression(tree.Root);
        }

        internal EvaluationResult EvaluateExpression(ExpressionSyntax expressionSyntax)
        {
            EvaluationResult result;
            switch (expressionSyntax.Type)
            {
                case SyntaxType.FloatLiteral:
                case SyntaxType.IntLiteral:
                case SyntaxType.LongLiteral:
                case SyntaxType.StringLiteral:
                case SyntaxType.TrueKeyword:
                case SyntaxType.FalseKeyword:
                case SyntaxType.NullKeyword:
                    result = EvaluateLiteralExpression(expressionSyntax as LiteralExpressionSyntax);
                    break;
                case SyntaxType.OutExpression:
                    result = EvaluateOutExpression(expressionSyntax as OutExpressionSyntax);
                    break;
                case SyntaxType.BinaryExpression:
                    result = EvaluateBinaryExpression(expressionSyntax as BinaryExpressionSyntax);
                    break;
                case SyntaxType.InvocationExpression:
                    result = EvaluateInvocationExpression(expressionSyntax as InvocationExpressionSyntax);
                    break;
                case SyntaxType.ConstructorExpression:
                    result = EvaluateConstructorExpression(expressionSyntax as ConstructorExpressionSyntax);
                    break;
                case SyntaxType.GenericInvocationExpression:
                    result = EvaluateGenericInvocationExpression(expressionSyntax as GenericInvocationExpressionSyntax);
                    break;
                case SyntaxType.IndexAccessExpression:
                    result = EvaluateIndexAccessExpression(expressionSyntax as IndexAccessExpressionSyntax);
                    break;
                case SyntaxType.MemberAccessExpression:
                    result = EvaluateMemberAccessExpression(expressionSyntax as MemberAccessExpressionSyntax);
                    break;
                case SyntaxType.AssignmentExpression:
                    result = EvaluateAssignmentExpression(expressionSyntax as AssignmentExpressionSyntax);
                    break;
                case SyntaxType.IdentifierName:
                    result = EvaluateIdentifierNameExpression(expressionSyntax as IdentifierNameSyntax);
                    break;
                case SyntaxType.TypeOfExpression:
                    result = EvaluateTypeOfExpression(expressionSyntax as TypeOfExpressionSyntax);
                    break;
                case SyntaxType.DeclarationExpression:
                    result = EvaluateDeclarationExpression(expressionSyntax as DeclarationExpressionSyntax);
                    break;
                case SyntaxType.UsingExpression:
                    result = EvaluateUsingExpression(expressionSyntax as UsingExpressionSyntax);
                    break;
                case SyntaxType.UnaryExpression:
                    result = EvaluateUnaryExpression(expressionSyntax as UnaryExpressionSyntax);
                    break;
                case SyntaxType.StringInterpolationExpression:
                    result = EvaluateStringInterpolationExpression(
                        expressionSyntax as StringInterpolationExpressionSyntax);
                    break;
                default:
                    throw new NotImplementedException($"{expressionSyntax.Type} evaluation not supported");
            }

            expressionSyntax.Value = result.Value;
            return result;
        }

        #endregion

        #region Field

        /// <summary>
        /// The literals' value has been resolved at lexing, just return it.
        /// </summary>
        private EvaluationResult EvaluateLiteralExpression(LiteralExpressionSyntax expressionSyntax)
        {
            return new EvaluationResult(expressionSyntax.Value, true);
        }

        private EvaluationResult EvaluateOutExpression(OutExpressionSyntax expression)
        {
            return new EvaluationResult(m_Context.SetVariable(expression.Identifier.Text, null), true);
        }

        /// <summary>
        /// 二元四则运算表达式
        /// </summary>
        private EvaluationResult EvaluateBinaryExpression(BinaryExpressionSyntax expressionSyntax)
        {
            throw new NotSupportedException(expressionSyntax.Type.ToString());
        }

        /// <summary>
        /// 参数列表求值
        /// </summary>
        private List<object> EvaluateArgumentList(ArgumentListSyntax argumentList)
        {
            var ret = new List<object>();
            foreach (var argument in argumentList.Arguments)
            {
                ret.Add(EvaluateExpression(argument.Expression).Value);
            }

            return ret;
        }

        /// <summary>
        /// 常规方法调用表达式求值
        /// </summary>
        private EvaluationResult EvaluateInvocationExpression(InvocationExpressionSyntax expression)
        {
            // Argument list
            var argumentValues = EvaluateArgumentList(expression.ParenthesisedArgumentList).ToArray();

            // Prefix
            var value = EvaluateExpression(expression.Expression).Value;

            // Built-in or Local function
            if (expression.Expression.Symbol is FunctionSymbol functionSymbol)
            {
                return new EvaluationResult(functionSymbol.MethodInfo.Invoke(null, argumentValues.ToArray()), true);
            }

            // Find out variables
            var outVariables = new Dictionary<int, VariableSymbol>();
            for (var i = 0; i < argumentValues.Length; i++)
            {
                if (argumentValues[i] is VariableSymbol symbol)
                {
                    argumentValues[i] = null;
                    outVariables.Add(i, symbol);
                }
            }

            var isStatic = value == null;
            var tns = expression.Expression.TypeOrNamespace;
            expression.TypeOrNamespace = isStatic ? expression.TypeOrNamespace : tns;
            var type = isStatic ? tns.Types.First() : value.GetType();
            var methodInfo = type.GetMethod(tns.ToBindName, isStatic ? StaticFlags : InstanceFlags, argumentValues);
            if (methodInfo == null)
            {
                throw new Exception(
                    $"Method '{TypeExtensions.FormatMethodName(tns.ToBindName, argumentValues)}' not found");
            }

            var ret = InvokeMethod(methodInfo, value, ref argumentValues);

            // Write back out variable values
            foreach (var kv in outVariables)
            {
                kv.Value.Value = argumentValues[kv.Key];
            }

            return ret;
        }

        private EvaluationResult EvaluateConstructorExpression(ConstructorExpressionSyntax expression)
        {
            if (expression.MethodExpression.Type == SyntaxType.GenericInvocationExpression)
            {
                EvaluateArgumentList(expression.GenericArgumentList);
                expression.ResolveGenericType();
            }

            var argumentValues = EvaluateArgumentList(expression.ArgumentList);
            EvaluateExpression(expression.TypeExpression);
            var tns = expression.TypeExpression.TypeOrNamespace;
            var argumentTypes = expression.GetArgumentTypes();
            ConstructorInfo ctor = null;
            foreach (var type in tns.Types)
            {
                ctor = type.GetConstructor(argumentTypes);
                expression.TypeOrNamespace = tns;
                if (ctor != null)
                {
                    break;
                }
            }

            if (ctor == null)
            {
                throw new Exception($"Constructor not found");
            }

            // 为实参补齐默认参数
            var paraCount = ctor.GetParameters().Length;
            for (var i = argumentValues.Count; i < paraCount; i++)
            {
                argumentValues.Add(Type.Missing);
            }

            return new EvaluationResult(ctor.Invoke(argumentValues.ToArray()), true);
        }

        /// <summary>
        /// 泛型方法调用表达式求值
        /// </summary>
        private EvaluationResult EvaluateGenericInvocationExpression(GenericInvocationExpressionSyntax expression)
        {
            // 参数求值
            EvaluateArgumentList(expression.GenericArgumentList);
            // 泛型参数求值
            var argumentValues = EvaluateArgumentList(expression.ParenthesisedArgumentList).ToArray();
            // 前缀表达式求值
            var value = EvaluateExpression(expression.Expression).Value;

            var genericArgumentTypes = expression.GetGenericArgumentTypes();
            var argumentTypes = expression.GetArgumentTypes();
            var tns = expression.Expression.TypeOrNamespace;
            MethodInfo methodInfo = null;
            if (value != null)
            {
                methodInfo = value.GetType().GetGenericMethod(tns.ToBindName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.FlattenHierarchy,
                    genericArgumentTypes, argumentTypes, null);
            }
            else
            {
                foreach (var type in tns.Types)
                {
                    methodInfo = type.GetGenericMethod(tns.ToBindName, StaticFlags, genericArgumentTypes,
                        argumentTypes, null);
                    expression.TypeOrNamespace = tns;
                }
            }

            if (methodInfo == null)
            {
                throw new Exception($"Failed to find generic method: {tns.ToBindName}");
            }

            return InvokeMethod(methodInfo, value, ref argumentValues);
        }

        private static EvaluationResult InvokeMethod(MethodInfo method, object thisValue, ref object[] argumentValues)
        {
            var ret = method.Invoke(thisValue, ref argumentValues, true);
            return method.ReturnType == typeof(void) ? EvaluationResult.Void : new EvaluationResult(ret, true);
        }

        /// <summary>
        /// 容器的[]索引访问表达式求值
        /// </summary>
        private EvaluationResult EvaluateIndexAccessExpression(IndexAccessExpressionSyntax expression)
        {
            // 前置对象求值
            var obj = EvaluateExpression(expression.Expression).Value;
            if (obj == null)
            {
                throw new NullReferenceException();
            }

            var indexKey = EvaluateExpression(expression.Key).Value;
            var type = obj.GetType();
            var methodName = type.IsArray ? "GetValue" : "get_Item";
            var ret = obj.GetType().InvokeMember(methodName,
                InstanceFlags | BindingFlags.OptionalParamBinding | BindingFlags.InvokeMethod,
                null,
                obj, new[] { indexKey });
            return new EvaluationResult(ret, true);
        }

        private EvaluationResult EvaluateIdentifierNameExpression(IdentifierNameSyntax nameSyntax)
        {
            var name = nameSyntax.Identifier.Text;

            // Local variable
            var symbol = m_Context.GetSymbol(name);
            if (symbol != null)
            {
                nameSyntax.Symbol = symbol;
                if (symbol is VariableSymbol variableSymbol)
                {
                    var value = variableSymbol.Value;
                    if (value != null)
                    {
                        nameSyntax.TypeOrNamespace = new TypeOrNamespace
                        {
                            Namespace = value.GetType().Namespace,
                            Types = new List<Type>
                            {
                                value.GetType()
                            }
                        };
                    }

                    return new EvaluationResult(value, true);
                }

                return new EvaluationResult(symbol, true);
            }

            if (!m_Context.TryLookupTypeOrNamespace(name, out var @namespace, out var types))
            {
                @namespace = m_Context.IsNamespace(name) ? name : null;
                types = m_Context.LookupTypes(name);
            }

            if (@namespace == null && types == null)
            {
                throw new Exception($"Type or namespace {name} not found");
            }

            nameSyntax.TypeOrNamespace = new TypeOrNamespace
            {
                Namespace = @namespace,
                Types = types
            };

            return new EvaluationResult(null, false);
        }

        private EvaluationResult EvaluateTypeOfExpression(TypeOfExpressionSyntax expression)
        {
            EvaluateExpression(expression.TypeExpression);
            var tns = expression.TypeExpression.TypeOrNamespace;
            var type = tns.Types.First();
            if (type == null)
            {
                throw new Exception($"Type {expression.TypeExpression} not found");
            }

            return new EvaluationResult(type, true);
        }

        private EvaluationResult EvaluateDeclarationExpression(DeclarationExpressionSyntax expression)
        {
            m_Context.CreateVariable(expression.VariableName);
            return new EvaluationResult(EvaluateExpression(expression.AssignmentExpressionSyntax), true);
        }

        private EvaluationResult EvaluateUsingExpression(UsingExpressionSyntax expression)
        {
            EvaluateExpression(expression.ExpressionSyntax);
            var ns = expression.ExpressionSyntax.TypeOrNamespace.Namespace;
            if (!string.IsNullOrEmpty(ns) && m_Context.IsNamespace(ns))
            {
                m_Context.UsingNameSpace(ns);
            }

            return EvaluationResult.Void;
        }

        private EvaluationResult EvaluateUnaryExpression(UnaryExpressionSyntax expression)
        {
            var value = EvaluateExpression(expression.Operand).Value;
            if (value is int intValue)
            {
                return new EvaluationResult(intValue * -1, true);
            }

            if (value is long longValue)
            {
                return new EvaluationResult(longValue * -1, true);
            }

            if (value is float floatValue)
            {
                return new EvaluationResult(floatValue * -1, true);
            }

            if (value is double doubleValue)
            {
                return new EvaluationResult(doubleValue * -1, true);
            }

            return new EvaluationResult(value.GetType()
                .GetMethod("op_UnaryNegation", BindingFlags.Static | BindingFlags.Public)
                ?.Invoke(null, null), true);
        }

        private EvaluationResult EvaluateStringInterpolationExpression(StringInterpolationExpressionSyntax expression)
        {
            var text = (string) expression.StringLiteral.Value;
            var matches = Regex.Matches(text, "{.+?}");
            var builder = new StringBuilder();
            var index = 0;
            foreach (Match match in matches)
            {
                builder.Append(text.Substring(index, match.Index - index));
                builder.Append(Evaluate(match.Value.Substring(1, match.Value.Length - 2), out _).Value);
                index = match.Index + match.Length;
            }

            builder.Append(text.Substring(index));
            return new EvaluationResult(builder.ToString(), true);
        }

        private EvaluationResult EvaluateMemberAccessExpression(MemberAccessExpressionSyntax expression)
        {
            var prefix = expression.Expression;
            EvaluateExpression(prefix);
            var postfix = expression.Name;
            var postfixName = postfix.Identifier.Text;
            var tns = new TypeOrNamespace
            {
                ToBindName = postfixName
            };

            // 先处理prefix已经有值的情况
            if (prefix.Value != null)
            {
                expression.TypeOrNamespace = tns;
                // 这是一个方法调用
                if (expression.Parent is InvocationExpressionSyntax)
                {
                    // prefix.postfixName是一个方法
                    tns.Types = new List<Type> { prefix.Value.GetType() };
                    tns.Namespace = prefix.Value.GetType().Namespace;
                    return new EvaluationResult(prefix.Value, true);
                }

                // prefix.postfixName是一个字段或属性
                if (ReflectionUtilities.TryGetMemberValue(prefix.Value, postfixName, out var value))
                {
                    if (value != null)
                    {
                        tns.Types = new List<Type> { value.GetType() };
                        tns.Namespace = value.GetType().Namespace;
                    }

                    return new EvaluationResult(value, true);
                }

                // 成员未找到
                throw new Exception($"Member {postfixName} not found");
            }

            // 处理prefix已经找到Type的情况
            var qualifiedName = $"{prefix.TypeOrNamespace.Namespace}.{postfixName}";
            var types = m_Context.LookupTypes(qualifiedName);
            if (prefix.TypeOrNamespace.Types.Count > 0)
            {
                // 1.属性或字段(静态)
                foreach (var type in prefix.TypeOrNamespace.Types)
                {
                    if (!type.TryGetMemberValue(postfixName, out var value))
                    {
                        continue;
                    }

                    // 缩小Type的范围
                    if (value != null)
                    {
                        var newType = value.GetType();
                        tns.Types = new List<Type> { newType };
                        tns.Namespace = newType.Namespace;
                        expression.TypeOrNamespace = tns;
                    }

                    return new EvaluationResult(value, true);
                }

                // 2.嵌套类型
                if (types.Count > 0)
                {
                    tns.Types = types;
                    tns.Namespace = prefix.TypeOrNamespace.Namespace;
                    expression.TypeOrNamespace = tns;
                    return new EvaluationResult(null, false);
                }

                // 3.方法(静态)
                // 4.构造方法
                // 5.错误的访问
                // 保持不变,让实际的调用方来检测错误
                tns.Types = prefix.TypeOrNamespace.Types;
                tns.Namespace = prefix.TypeOrNamespace.Namespace;
                expression.TypeOrNamespace = tns;
                return new EvaluationResult(null, false);
            }

            // 还未找到Type的情况
            tns.Namespace = m_Context.IsNamespace(qualifiedName) ? qualifiedName : prefix.TypeOrNamespace.Namespace;
            tns.Types = types;
            expression.TypeOrNamespace = tns;
            return new EvaluationResult(null, false);
        }

        /// <summary>
        /// 赋值表达式求值
        /// </summary>
        private EvaluationResult EvaluateAssignmentExpression(AssignmentExpressionSyntax expression)
        {
            // 取左边的FieldInfo or PropertyInfo
            var left = expression.Left;
            var value = EvaluateExpression(expression.Right).Value;
            switch (left)
            {
                case MemberAccessExpressionSyntax memberAccessExpr:
                {
                    var leftValue = EvaluateExpression(memberAccessExpr.Expression).Value;
                    if (leftValue != null)
                    {
                        ReflectionUtilities.SetValue(leftValue, memberAccessExpr.Name.Identifier.Text, value);
                    }
                    else
                    {
                        var type = memberAccessExpr.Expression.TypeOrNamespace.Types.First();
                        type.SetValue(memberAccessExpr.Name.Identifier.Text, value);
                    }

                    break;
                }
                //声明并赋值
                case DeclarationExpressionSyntax declarationExpr:
                {
                    var leftValue = EvaluateExpression(declarationExpr).Value;
                    m_Context.SetVariable((string) leftValue, value);
                    break;
                }
                //赋值给临时变量
                case IdentifierNameSyntax identifierNameExpr:
                {
                    m_Context.SetVariable(identifierNameExpr.Identifier.Text, value);
                    break;
                }
            }

            return new EvaluationResult(value, true);
        }

        #endregion

        #region Field

        private readonly Context m_Context;

        private const BindingFlags DefaultFlags =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        private const BindingFlags StaticFlags = BindingFlags.Static | DefaultFlags;

        private const BindingFlags InstanceFlags = BindingFlags.Instance | DefaultFlags;

        #endregion
    }
}