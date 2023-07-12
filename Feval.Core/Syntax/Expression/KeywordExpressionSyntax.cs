using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class KeywordExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.KeywordExpression;

        public SyntaxToken KeywordToken { get; }

        #endregion

        #region Interface

        public KeywordExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken keywordToken) : base(syntaxTree)
        {
            KeywordToken = keywordToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return KeywordToken;
        }

        #endregion
    }
}