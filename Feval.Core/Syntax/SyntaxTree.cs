using System.Collections.Generic;

namespace Feval.Syntax
{
    public sealed class SyntaxTree
    {
        #region Property

        public ExpressionSyntax Root { get; }

        public IEnumerable<SyntaxToken> Tokens => m_Parser.Tokens;

        public string Text { get; }

        #endregion

        #region Interface

        public static SyntaxTree Parse(string text)
        {
            return new SyntaxTree(text);
        }

        public SyntaxNode GetParent(SyntaxNode node)
        {
            if (m_Parents == null)
            {
                m_Parents = CreateParentsDictionary(Root);
            }

            return m_Parents[node];
        }

        #endregion

        #region Method

        private SyntaxTree(string text)
        {
            Text = text;
            m_Parser = new Parser(this);
            Root = m_Parser.Parse();
        }

        private static Dictionary<SyntaxNode, SyntaxNode> CreateParentsDictionary(ExpressionSyntax root)
        {
            var result = new Dictionary<SyntaxNode, SyntaxNode> { { root, null } };
            CreateParentsDictionary(result, root);
            return result;
        }

        private static void CreateParentsDictionary(Dictionary<SyntaxNode, SyntaxNode> result, SyntaxNode node)
        {
            foreach (var child in node.GetChildren())
            {
                result.Add(child, node);
                CreateParentsDictionary(result, child);
            }
        }

        #endregion

        #region Field

        private readonly Parser m_Parser;

        private Dictionary<SyntaxNode, SyntaxNode> m_Parents;

        #endregion
    }
}