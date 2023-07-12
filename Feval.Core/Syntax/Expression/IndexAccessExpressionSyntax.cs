using System.Collections.Generic;

namespace Feval.Syntax
{
    /// <summary>
    /// Collection [] operator expression
    /// </summary>
    internal sealed class IndexAccessExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.IndexAccessExpression;

        public ExpressionSyntax Expression { get; }

        public SyntaxToken OpenSquareBracket { get; }

        public ExpressionSyntax Key { get; }

        public SyntaxToken CloseSquareBracket { get; }

        #endregion

        #region Interface

        public IndexAccessExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression,
            SyntaxToken openSquareBracket, ExpressionSyntax key,
            SyntaxToken closeSquareBracket) : base(syntaxTree)
        {
            Expression = expression;
            OpenSquareBracket = openSquareBracket;
            Key = key;
            CloseSquareBracket = closeSquareBracket;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return OpenSquareBracket;
            yield return Key;
            yield return CloseSquareBracket;
        }

        #endregion
    }
}