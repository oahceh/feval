using System.Collections.Generic;

namespace Feval.Syntax
{
    internal class DeclarationExpressionSyntax : ExpressionSyntax
    {
        public string VariableName => AssignmentExpressionSyntax.Left.Text;

        public KeywordExpressionSyntax VarKeywordToken { get; }

        public AssignmentExpressionSyntax AssignmentExpressionSyntax { get; }

        public DeclarationExpressionSyntax(SyntaxTree syntaxTree, KeywordExpressionSyntax varKeyWordToken,
            AssignmentExpressionSyntax assignmentExpressionSyntax) : base(syntaxTree)
        {
            VarKeywordToken = varKeyWordToken;
            AssignmentExpressionSyntax = assignmentExpressionSyntax;
        }

        public override SyntaxType Type => SyntaxType.DeclarationExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return VarKeywordToken;
            yield return AssignmentExpressionSyntax;
        }
    }
}