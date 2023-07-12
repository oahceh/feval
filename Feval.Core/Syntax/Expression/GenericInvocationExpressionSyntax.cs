using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class GenericInvocationExpressionSyntax : InvocationExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.GenericInvocationExpression;

        public GenericArgumentListSyntax GenericArgumentList { get; }

        #endregion

        #region Interface

        public GenericInvocationExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression,
            GenericArgumentListSyntax genericArgumentList,
            ParenthesisedArgumentListSyntax parenthesisedArgumentList) : base(syntaxTree, expression, parenthesisedArgumentList)
        {
            GenericArgumentList = genericArgumentList;
        }

        public Type[] GetGenericArgumentTypes()
        {
            return GenericArgumentList.GetArgumentTypes();
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return GenericArgumentList;
            yield return ParenthesisedArgumentList;
        }

        #endregion
    }
}