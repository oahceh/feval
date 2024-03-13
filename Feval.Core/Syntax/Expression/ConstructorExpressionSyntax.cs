using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class ConstructorExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.ConstructorExpression;

        public KeywordExpressionSyntax New { get; }

        public InvocationExpressionSyntax MethodExpression { get; }

        public ExpressionSyntax TypeExpression => MethodExpression.Expression;

        public ParenthesisedArgumentListSyntax ArgumentList => MethodExpression.ParenthesisedArgumentList;

        public GenericArgumentListSyntax GenericArgumentList =>
            (MethodExpression as GenericInvocationExpressionSyntax)?.GenericArgumentList;

        #endregion

        #region Interface

        public ConstructorExpressionSyntax(SyntaxTree syntaxTree, KeywordExpressionSyntax newKeyword,
            InvocationExpressionSyntax methodExpression) :
            base(syntaxTree)
        {
            New = newKeyword;
            MethodExpression = methodExpression;
        }

        public Type[] GetArgumentTypes()
        {
            return MethodExpression.GetArgumentTypes();
        }

        public void ResolveGenericTypeName()
        {
            var rightestIdentifier = (MethodExpression.Expression as IdentifierNameSyntax) ??
                                     (MethodExpression.Expression as MemberAccessExpressionSyntax).Name;
            rightestIdentifier.SetText(
                $"{rightestIdentifier.Text}`{(MethodExpression as GenericInvocationExpressionSyntax).GenericArgumentList.Count}");
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return New;
            yield return MethodExpression;
        }

        #endregion
    }
}