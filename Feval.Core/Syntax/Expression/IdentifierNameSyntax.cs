using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class IdentifierNameSyntax : ExpressionSyntax
    {
        #region Interface

        public override SyntaxType Type => SyntaxType.IdentifierName;

        public SyntaxToken Identifier { get; }

        public override string Text => Identifier.Text;

        #endregion

        #region Interface

        public IdentifierNameSyntax(SyntaxTree syntaxTree, SyntaxToken identifier) : base(syntaxTree)
        {
            Identifier = identifier;
        }

        public void SetText(string text)
        {
            Identifier.Text = text;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
        }

        #endregion
    }
}