using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    internal static class SyntaxDefinition
    {
        #region Interface

        internal static bool TryGetKeywordSyntaxType(string text, out SyntaxType type)
        {
            return m_Keywords.TryGetValue(text, out type);
        }

        internal static bool TryGetTypeKeyword(string text, out Type type)
        {
            return m_TypeKeywords.TryGetValue(text, out type);
        }

        internal static int GetBinaryOperatorPriority(SyntaxType type)
        {
            return m_BinaryOperatorPriorities.TryGetValue(type, out var ret) ? ret : 0;
        }

        #endregion

        #region Field

        private static readonly Dictionary<string, SyntaxType> m_Keywords = new()
        {
            { "new", SyntaxType.NewKeyword },
            { "true", SyntaxType.TrueKeyword },
            { "false", SyntaxType.FalseKeyword },
            { "typeof", SyntaxType.TypeOfKeyword },
            { "null", SyntaxType.NullKeyword },
            { "var", SyntaxType.VarKeyword },
            { "using", SyntaxType.UsingKeyword },
            { "out", SyntaxType.OutKeyword }
        };

        private static readonly Dictionary<string, Type> m_TypeKeywords = new()
        {
            { "string", typeof(string) },
            { "int", typeof(int) },
            { "float", typeof(float) },
            { "long", typeof(long) },
            { "double", typeof(double) }
        };

        private static readonly Dictionary<SyntaxType, int> m_BinaryOperatorPriorities = new()
        {
            { SyntaxType.DotToken, 6 },
            { SyntaxType.MultiplyToken, 5 },
            { SyntaxType.DivideToken, 5 },
            { SyntaxType.PlusToken, 4 },
            { SyntaxType.MinusToken, 4 },
            { SyntaxType.PipeToken, 3 }
        };

        #endregion
    }
}