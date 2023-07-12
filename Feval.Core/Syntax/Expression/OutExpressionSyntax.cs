using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class OutExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxType Type => SyntaxType.OutExpression;

        public KeywordExpressionSyntax OutKeyword { get; }

        public IdentifierNameSyntax Identifier { get; }

        public OutExpressionSyntax(SyntaxTree syntaxTree, KeywordExpressionSyntax outKeyword,
            IdentifierNameSyntax identifier) : base(syntaxTree)
        {
            OutKeyword = outKeyword;
            Identifier = identifier;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OutKeyword;
            yield return Identifier;
        }
    }
}