using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override string Text => $"{Expression.Text}.{Name.Text}";

        public override SyntaxType Type => SyntaxType.MemberAccessExpression;

        public ExpressionSyntax Expression { get; }

        public SyntaxToken OperatorToken { get; }

        public IdentifierNameSyntax Name { get; }

        #endregion

        #region Interface

        public MemberAccessExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression,
            SyntaxToken operatorToken,
            IdentifierNameSyntax name) :
            base(syntaxTree)
        {
            Expression = expression;
            OperatorToken = operatorToken;
            Name = name;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
            yield return OperatorToken;
            yield return Name;
        }

        #endregion
    }
}