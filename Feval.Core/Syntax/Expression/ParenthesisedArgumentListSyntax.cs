using System.Collections.Generic;

namespace Feval.Syntax
{
    internal class ParenthesisedArgumentListSyntax : ArgumentListSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.ArgumentList;

        public SyntaxToken OpenParenToken { get; }

        public SyntaxToken CloseParenToken { get; }

        #endregion

        public ParenthesisedArgumentListSyntax(SyntaxTree syntaxTree, SyntaxToken openParenToken,
            SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeParenToken) : base(syntaxTree, arguments)
        {
            OpenParenToken = openParenToken;
            CloseParenToken = closeParenToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            foreach (var child in Arguments.GetWithSeparators())
            {
                yield return child;
            }

            yield return CloseParenToken;
        }
    }
}