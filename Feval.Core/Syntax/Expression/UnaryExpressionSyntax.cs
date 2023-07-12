using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class UnaryExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxType Type => SyntaxType.UnaryExpression;

        public SyntaxToken Operator { get; }

        public ExpressionSyntax Operand { get; }

        public UnaryExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken operatorToken, ExpressionSyntax operand) :
            base(syntaxTree)
        {
            Operator = operatorToken;
            Operand = operand;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Operator;
            yield return Operand;
        }
    }
}