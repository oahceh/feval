using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    public class ArgumentSyntax : SyntaxNode
    {
        #region Property

        public override SyntaxType Type => SyntaxType.Argument;

        public ExpressionSyntax Expression { get; }

        #endregion

        #region Interface

        public ArgumentSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression) : base(syntaxTree)
        {
            Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }

        #endregion
    }
}