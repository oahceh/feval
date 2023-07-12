using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    public class SyntaxToken : SyntaxNode
    {
        #region Property

        public int Position { get; }

        public string Text { get; set; }

        public override object Value { get; set; }

        public override SyntaxType Type { get; }

        #endregion

        #region Interface

        public SyntaxToken(SyntaxTree syntaxTree, SyntaxType type, int position, string text, object value = null) :
            base(syntaxTree)
        {
            Type = type;
            Position = position;
            Text = text;
            Value = value;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Array.Empty<SyntaxNode>();
        }

        public override string ToString()
        {
            return $"({Type}, {Text}, {Value ?? "~"})";
        }

        #endregion
    }
}