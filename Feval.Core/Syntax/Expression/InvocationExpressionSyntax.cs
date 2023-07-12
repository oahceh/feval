using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    internal class InvocationExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.InvocationExpression;

        public ExpressionSyntax Expression { get; }

        public ParenthesisedArgumentListSyntax ParenthesisedArgumentList { get; }
        
        #endregion

        #region Interface

        public InvocationExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression,
            ParenthesisedArgumentListSyntax parenthesisedArgumentList) :
            base(syntaxTree)
        {
            Expression = expression;
            ParenthesisedArgumentList = parenthesisedArgumentList;
        }

        public Type[] GetArgumentTypes()
        {
            return ParenthesisedArgumentList.GetArgumentTypes();
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return ParenthesisedArgumentList;
        }

        #endregion
    }
}