using System.Collections.Generic;

namespace Feval.Syntax
{
    internal sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.AssignmentExpression;

        public ExpressionSyntax Left { get; }

        public SyntaxToken EqualsToken { get; }

        public ExpressionSyntax Right { get; }

        #endregion

        #region Method

        public AssignmentExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax left,
            SyntaxToken equalsToken,
            ExpressionSyntax right) : base(syntaxTree)
        {
            Left = left;
            EqualsToken = equalsToken;
            Right = right;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return EqualsToken;
            yield return Right;
        }

        #endregion
    }
}