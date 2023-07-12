using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class StringInterpolationExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxType Type => SyntaxType.StringInterpolationExpression;

        public SyntaxToken DollarToken { get; }

        public LiteralExpressionSyntax StringLiteral { get; }

        public StringInterpolationExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken dollarToken,
            LiteralExpressionSyntax stringLiteral) : base(syntaxTree)
        {
            DollarToken = dollarToken;
            StringLiteral = stringLiteral;
        }


        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return DollarToken;
            yield return StringLiteral;
        }
    }
}