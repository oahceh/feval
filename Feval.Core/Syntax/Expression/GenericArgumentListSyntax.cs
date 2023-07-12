using System;
using System.Collections.Generic;
using System.Linq;

namespace Feval.Syntax
{
    internal sealed class GenericArgumentListSyntax : ArgumentListSyntax
    {
        #region Property

        public override SyntaxType Type => SyntaxType.GenericArgumentList;

        public SyntaxToken OpenAngleBracketToken { get; }

        public SyntaxToken CloseAngleBracketToken { get; }

        #endregion

        #region Interface

        public GenericArgumentListSyntax(SyntaxTree syntaxTree, SyntaxToken openAngleBracketToken,
            SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeAngleBracketToken) : base(syntaxTree,
            arguments)
        {
            OpenAngleBracketToken = openAngleBracketToken;
            CloseAngleBracketToken = closeAngleBracketToken;
        }

        public override Type[] GetArgumentTypes()
        {
            var ret = new Type[Count];
            for (var i = 0; i < Arguments.Count; i++)
            {
                ret[i] = Arguments[i].Expression.TypeOrNamespace.Types.First();
            }

            return ret;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenAngleBracketToken;
            foreach (var child in Arguments.GetWithSeparators())
            {
                yield return child;
            }

            yield return CloseAngleBracketToken;
        }

        #endregion
    }
}