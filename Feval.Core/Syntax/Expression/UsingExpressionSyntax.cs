using System.Collections.Generic;

namespace Feval.Syntax
{
    internal class UsingExpressionSyntax: ExpressionSyntax
    {
        public KeywordExpressionSyntax UsingKeywordToken { get; }

        public ExpressionSyntax ExpressionSyntax { get; }

        public UsingExpressionSyntax(SyntaxTree syntaxTree, KeywordExpressionSyntax usingKeyWordToken,
            ExpressionSyntax expressionSyntax) : base(syntaxTree)
        {
            UsingKeywordToken = usingKeyWordToken;
            ExpressionSyntax = expressionSyntax;
        }

        public override SyntaxType Type => SyntaxType.UsingExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return UsingKeywordToken;
            yield return ExpressionSyntax;
        }
    }
}