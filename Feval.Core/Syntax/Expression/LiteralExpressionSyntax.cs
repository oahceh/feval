using System.Collections.Generic;

namespace Feval.Syntax
{
    public class LiteralExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public SyntaxToken LiteralSyntaxToken { get; }

        public override object Value => LiteralSyntaxToken.Value;

        public override SyntaxType Type => LiteralSyntaxToken.Type;

        #endregion

        #region Interface

        public LiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken literalSyntaxToken) : base(syntaxTree)
        {
            LiteralSyntaxToken = literalSyntaxToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralSyntaxToken;
        }

        #endregion
    }
}