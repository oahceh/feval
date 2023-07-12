namespace Feval.Syntax
{
    public abstract class ExpressionSyntax : SyntaxNode
    {
        public virtual string Text { get; }

        public TypeOrNamespace TypeOrNamespace { get; set; }

        public Symbol Symbol { get; set; }

        protected ExpressionSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }
    }
}