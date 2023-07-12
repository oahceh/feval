using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    internal abstract class ArgumentListSyntax : SyntaxNode
    {
        #region Property

        public SeparatedSyntaxList<ArgumentSyntax> Arguments { get; }

        public int Count => Arguments.Count;

        #endregion

        #region Interface

        public virtual Type[] GetArgumentTypes()
        {
            var ret = new Type[Count];
            for (var i = 0; i < Arguments.Count; i++)
            {
                ret[i] = Arguments[i].Expression.Value?.GetType();
            }

            return ret;
        }

        #endregion

        #region Method

        protected ArgumentListSyntax(SyntaxTree syntaxTree, SeparatedSyntaxList<ArgumentSyntax> arguments) :
            base(syntaxTree)
        {
            Arguments = arguments;
        }

        #endregion
    }
}