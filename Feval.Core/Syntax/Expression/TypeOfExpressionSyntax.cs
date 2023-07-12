using System.Collections.Generic;

namespace Feval.Syntax
{
    internal class TypeOfExpressionSyntax : ExpressionSyntax
    {
        #region Interface

        public override SyntaxType Type => SyntaxType.TypeOfExpression;

        public KeywordExpressionSyntax TypeOfKeywordToken { get; }

        public SyntaxToken OpenParenthesisToken { get; }

        public ExpressionSyntax TypeExpression { get; }

        public SyntaxToken CloseParenthesisToken { get; }

        #endregion

        #region Interface

        public TypeOfExpressionSyntax(SyntaxTree syntaxTree, KeywordExpressionSyntax typeOfKeywordToken,
            SyntaxToken openParenthesisToken, ExpressionSyntax typeExpression,
            SyntaxToken closeParenthesisToken) : base(syntaxTree)
        {
            TypeOfKeywordToken = typeOfKeywordToken;
            OpenParenthesisToken = openParenthesisToken;
            TypeExpression = typeExpression;
            CloseParenthesisToken = closeParenthesisToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeOfKeywordToken;
            yield return OpenParenthesisToken;
            yield return TypeExpression;
            yield return CloseParenthesisToken;
        }

        #endregion
    }
}