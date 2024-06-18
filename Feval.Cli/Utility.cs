using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Feval.Syntax;

namespace Feval.Cli
{
    internal static class Utility
    {
        #region Interface

        internal static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "",
            bool isLast = true)
        {
            var isToConsole = writer == Console.Out;
            var token = node as SyntaxToken;
            var isToken = token != null;

            var tokenMarker = isLast ? "└──" : "├──";
            if (isToConsole)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            writer.Write(indent);
            writer.Write(tokenMarker);
            if (isToConsole)
            {
                Console.ForegroundColor = isToken ? ConsoleColor.Blue : ConsoleColor.Cyan;
            }

            writer.Write(isToken ? $"{token.Type} {token.Text ?? string.Empty}" : node.GetType().Name);

            if (isToken && token.Value != null)
            {
                writer.Write(" ");
                writer.Write(token.Value);
            }

            if (isToConsole)
            {
                Console.ResetColor();
            }

            writer.WriteLine();
            indent += isLast ? "   " : "│  ";

            var children = node.GetChildren();
            var syntaxNodes = children.ToList();
            var lastChild = syntaxNodes.LastOrDefault();
            foreach (var child in syntaxNodes)
            {
                PrettyPrint(writer, child, indent, child == lastChild);
            }
        }

        internal static void PrintTokens(TextWriter writer, IEnumerable<SyntaxToken> tokens)
        {
            foreach (var token in tokens)
            {
                writer.WriteLine(token.Type == SyntaxType.EndOfFile
                    ? $"<{token.Type}>"
                    : $"<{token.Type}: {token.Text}>");
            }
        }

        #endregion
    }
}