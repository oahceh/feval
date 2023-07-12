using System.Collections.Generic;

namespace Feval.Syntax
{
    /// <summary>
    /// 二元表达式
    /// 如 E0 + E1, E0.E1等
    /// </summary>
    public sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.BinaryExpression;

        public ExpressionSyntax Left { get; }

        public SyntaxToken Operator { get; }

        public ExpressionSyntax Right { get; }

        #endregion

        #region Interface

        public BinaryExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax left, SyntaxToken op,
            ExpressionSyntax right) : base(syntaxTree)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return Operator;
            yield return Right;
        }

        #endregion
    }
}