using System;
using System.Text;

namespace Feval.Syntax
{
    public class Lexer
    {
        #region Interface

        public Lexer(SyntaxTree syntaxTree)
        {
            m_SyntaxTree = syntaxTree;
            m_Text = syntaxTree.Text;
        }

        public SyntaxToken NextToken()
        {
            ReadWhitespace();

            var start = m_Position;
            {
                ReadToken();
            }
            var type = m_Type;
            var length = m_Position - start;
            var text = m_Text.Substring(start, length);

            ReadWhitespace();

            return new SyntaxToken(m_SyntaxTree, type, start, text, m_Value);
        }

        #endregion

        #region Method

        private void ReadToken()
        {
            m_Type = SyntaxType.Invalid;
            m_Value = null;
            switch (Current)
            {
                case '\0':
                    m_Type = SyntaxType.EndOfFile;
                    break;
                case '(':
                    m_Type = SyntaxType.OpenParenthesisToken;
                    MoveNext();
                    break;
                case ')':
                    m_Type = SyntaxType.CloseParenthesisToken;
                    MoveNext();
                    break;
                case '<':
                    m_Type = SyntaxType.OpenAngleBracketToken;
                    MoveNext();
                    break;
                case '>':
                    m_Type = SyntaxType.CloseAngleBracketToken;
                    MoveNext();
                    break;
                case '[':
                    m_Type = SyntaxType.OpenSquareBracketToken;
                    MoveNext();
                    break;
                case ']':
                    m_Type = SyntaxType.CloseSquareBracketToken;
                    MoveNext();
                    break;
                case '+':
                    m_Type = SyntaxType.PlusToken;
                    MoveNext();
                    break;
                case '-':
                    m_Type = SyntaxType.MinusToken;
                    MoveNext();
                    break;
                case '*':
                    m_Type = SyntaxType.MultiplyToken;
                    MoveNext();
                    break;
                case '/':
                    m_Type = SyntaxType.DivideToken;
                    break;
                case ',':
                    m_Type = SyntaxType.CommaToken;
                    MoveNext();
                    break;
                case '=':
                    m_Type = Peek(1) == '=' ? SyntaxType.EqualsEqualsToken : SyntaxType.EqualsToken;
                    MoveNext();
                    break;
                case '.':
                    m_Type = SyntaxType.DotToken;
                    MoveNext();
                    break;
                case '$':
                    m_Type = SyntaxType.DollarToken;
                    MoveNext();
                    break;
                case '`':
                    m_Type = SyntaxType.BackquoteToken;
                    MoveNext();
                    break;
                case '"':
                    ReadString();
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    ReadNumber();
                    break;
                case '_':
                    ReadIdentifierOrKeyword();
                    break;
                default:
                    if (char.IsLetter(Current))
                    {
                        ReadIdentifierOrKeyword();
                    }
                    else
                    {
                        throw new Exception($"Illegal character: {Current}");
                    }

                    break;
            }
        }

        private int MoveNext(int offset = 1)
        {
            var ret = m_Position;
            m_Position += offset;
            return ret;
        }

        private void ReadWhitespace()
        {
            while (char.IsWhiteSpace(Current))
            {
                MoveNext();
            }

            m_Type = SyntaxType.WhitespaceToken;
        }

        private void ReadString()
        {
            MoveNext();
            StringBuilder.Clear();
            var done = false;
            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        throw new Exception($"Syntax Error: illegal string {StringBuilder}");
                    case '"':
                        if (Peek(1) == '"')
                        {
                            StringBuilder.Append(Current);
                            MoveNext(2);
                        }
                        else
                        {
                            MoveNext();
                            done = true;
                        }

                        break;
                    default:
                        StringBuilder.Append(Current);
                        MoveNext();
                        break;
                }
            }

            m_Type = SyntaxType.StringLiteral;
            m_Value = StringBuilder.ToString();
        }

        private void ReadNumber()
        {
            var start = m_Position;
            var meetDot = false;
            while (char.IsDigit(Current) || Current == '.' && !meetDot)
            {
                if (Current == '.')
                {
                    meetDot = true;
                }

                MoveNext();
            }

            var text = m_Text.Substring(start, m_Position - start);
            if (meetDot)
            {
                if (!float.TryParse(text, out var value))
                {
                    throw new Exception($"Syntax Error: illegal float number {text}");
                }

                m_Type = SyntaxType.FloatLiteral;
                m_Value = value;
            }
            else
            {
                if (int.TryParse(text, out var intValue))
                {
                    m_Type = SyntaxType.IntLiteral;
                    m_Value = intValue;
                }
                else if (long.TryParse(text, out var longValue))
                {
                    m_Type = SyntaxType.LongLiteral;
                    m_Value = longValue;
                }
                else
                {
                    throw new Exception($"Syntax Error: illegal number {text}");
                }
            }
        }

        /// <summary>
        /// Read identifier or keyword token, the index pointer will be placed at the next of this token.
        /// <p> DO NOT CALL <c>MoveNext()</c> AFTER THIS TOKEN READ.</p>
        /// </summary>
        private void ReadIdentifierOrKeyword()
        {
            var start = m_Position;
            while (char.IsLetter(Current) || Current == '_' || char.IsDigit(Current))
            {
                MoveNext();
            }

            var text = m_Text.Substring(start, m_Position - start);
            var isKeyword = SyntaxDefinition.TryGetKeywordSyntaxType(text, out m_Type);
            m_Type = isKeyword ? m_Type : SyntaxType.IdentifierToken;
            if (isKeyword)
            {
                switch (m_Type)
                {
                    case SyntaxType.TrueKeyword:
                        m_Value = true;
                        break;
                    case SyntaxType.FalseKeyword:
                        m_Value = false;
                        break;
                    case SyntaxType.NullKeyword:
                        m_Value = null;
                        break;
                }
            }
        }

        private char Peek(int offset)
        {
            var index = m_Position + offset;
            return index >= m_Text.Length ? '\0' : m_Text[index];
        }

        #endregion

        #region Field

        private readonly SyntaxTree m_SyntaxTree;

        private char Current => Peek(0);

        private readonly string m_Text;

        private int m_Position;

        private SyntaxType m_Type;

        private object m_Value;

        private StringBuilder StringBuilder => m_StringBuilder ?? (m_StringBuilder = new StringBuilder());

        private StringBuilder m_StringBuilder;

        #endregion
    }
}