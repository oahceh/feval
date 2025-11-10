using System.Text.RegularExpressions;
using Feval.Syntax;

namespace Feval.Cli;

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


    /// <summary>
    /// 验证IPv4地址及可选端口号
    /// </summary>
    /// <param name="input">要验证的字符串</param>
    /// <param name="ipAddress">输出的IP地址部分</param>
    /// <param name="port">输出的端口号(未指定时为0)</param>
    /// <returns>验证是否成功</returns>
    public static bool TryParseIPAddress(this string input, out string ipAddress, out int port)
    {
        ipAddress = string.Empty;
        port = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = m_IPAddressRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        // 提取完整的IP地址部分
        ipAddress = match.Groups["ip"].Value;

        // 提取并验证端口号
        if (match.Groups["port"].Success)
        {
            if (!int.TryParse(match.Groups["port"].Value, out var portNumber))
            {
                return false;
            }

            // 端口号范围：1-65535
            if (portNumber < 1 || portNumber > 65535)
            {
                return false;
            }

            port = portNumber;
        }

        return true;
    }

    public static bool IsValidIPAddress(this string input)
    {
        return TryParseIPAddress(input, out _, out _);
    }

    #endregion

    #region Field

    private static readonly Regex m_IPAddressRegex = new(
        @"^(?<ip>((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))(?::(?<port>\d{1,5}))?$",
        RegexOptions.Compiled);

    #endregion
}