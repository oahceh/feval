using System.Collections.Generic;

namespace Feval.Syntax
{
    public abstract class SyntaxNode
    {
        #region Property

        public abstract SyntaxType Type { get; }

        public SyntaxTree SyntaxTree { get; }

        public SyntaxNode Parent => SyntaxTree.GetParent(this);

        public virtual object Value { get; set; }

        #endregion

        #region Interface

        public abstract IEnumerable<SyntaxNode> GetChildren();

        #endregion

        #region Method

        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        #endregion
    }
}