using System;
using System.Collections;
using System.Collections.Generic;

namespace Feval.Syntax
{
    public abstract class SeparatedSyntaxList
    {
        private protected SeparatedSyntaxList()
        {
        }

        public abstract List<SyntaxNode> GetWithSeparators();
    }

    public sealed class SeparatedSyntaxList<T> : SeparatedSyntaxList, IEnumerable<T>
        where T : SyntaxNode
    {
        private readonly List<SyntaxNode> m_NodesAndSeparators;

        internal SeparatedSyntaxList(List<SyntaxNode> nodesAndSeparators)
        {
            m_NodesAndSeparators = nodesAndSeparators;
        }

        public int Count => (m_NodesAndSeparators.Count + 1) / 2;

        public T this[int index] => (T) m_NodesAndSeparators[index * 2];

        public SyntaxToken GetSeparator(int index)
        {
            if (index < 0 || index >= Count - 1)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (SyntaxToken) m_NodesAndSeparators[index * 2 + 1];
        }

        public override List<SyntaxNode> GetWithSeparators() => m_NodesAndSeparators;

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}