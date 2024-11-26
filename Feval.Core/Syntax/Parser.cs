using System;
using System.Collections.Generic;

namespace Feval.Syntax
{
    public class Parser
    {
        #region Property

        public SyntaxToken[] Tokens { get; }

        #endregion

        #region Interface

        public Parser(SyntaxTree syntaxTree)
        {
            var tokens = new List<SyntaxToken>();
            var invalidTokens = new List<SyntaxToken>();
            var lexer = new Lexer(syntaxTree);
            SyntaxToken syntaxToken;
            do
            {
                syntaxToken = lexer.NextToken();
                if (syntaxToken.Type == SyntaxType.Invalid)
                {
                    invalidTokens.Add(syntaxToken);
                }
                else
                {
                    if (invalidTokens.Count > 0)
                    {
                        // TODO Bad token process...
                    }

                    tokens.Add(syntaxToken);
                }
            } while (syntaxToken.Type != SyntaxType.EndOfFile);

            m_SyntaxTree = syntaxTree;
            Tokens = tokens.ToArray();
        }

        internal ExpressionSyntax Parse()
        {
            return ParseBinaryExpression();
        }

        #endregion

        #region Method

        private SyntaxToken PeekToken(int offset)
        {
            var index = m_Position + offset;
            return index >= Tokens.Length ? Tokens[Tokens.Length - 1] : Tokens[index];
        }

        private SyntaxToken EatToken()
        {
            var current = CurrentToken;
            m_Position++;
            return current;
        }

        private SyntaxToken ExpectToken(SyntaxType type)
        {
            if (CurrentToken.Type == type)
            {
                return EatToken();
            }

            throw new Exception($"Syntax Error: unexpected token '{CurrentToken.Text}' while {type} expected");
        }

        private ExpressionSyntax ParseExpression()
        {
            return ParsePostfixExpression(ParseTermWithoutPostfix());
        }

        /// <summary>
        /// 二元运算表达式文法标准解法
        /// E => EOE
        /// O => +|-|*|/
        /// E => t
        /// </summary>
        /// <returns></returns>
        private ExpressionSyntax ParseBinaryExpression(int parentPriority = 0)
        {
            var left = ParseExpression();
            while (true)
            {
                var priority = SyntaxDefinition.GetBinaryOperatorPriority(CurrentToken.Type);
                // 当前Token不是二元运算符或低于上一层运算符的优先级, 则返回上一层，同上一层继续尝试结合
                if (priority == 0 || priority < parentPriority)
                {
                    break;
                }

                var op = EatToken();
                var right = ParseBinaryExpression(priority);
                left = new BinaryExpressionSyntax(m_SyntaxTree, left, op, right);
            }

            return left;
        }

        private ExpressionSyntax ParseTokenLiteralExpression(SyntaxType type)
        {
            return new LiteralExpressionSyntax(m_SyntaxTree, ExpectToken(type));
        }

        private KeywordExpressionSyntax ParseKeywordExpression(SyntaxToken keywordToken)
        {
            EatToken();
            return new KeywordExpressionSyntax(m_SyntaxTree, keywordToken);
        }

        private IdentifierNameSyntax ParseIdentifierName()
        {
            return new IdentifierNameSyntax(m_SyntaxTree, ExpectToken(SyntaxType.IdentifierToken));
        }

        private ExpressionSyntax ParseTermWithoutPostfix()
        {
            switch (CurrentToken.Type)
            {
                case SyntaxType.IdentifierToken:
                    return ParseIdentifierName();
                // Literals...
                case SyntaxType.FloatLiteral:
                case SyntaxType.IntLiteral:
                case SyntaxType.LongLiteral:
                case SyntaxType.StringLiteral:
                case SyntaxType.TrueKeyword:
                case SyntaxType.FalseKeyword:
                case SyntaxType.NullKeyword:
                    return ParseTokenLiteralExpression(CurrentToken.Type);
                // Keywords...
                case SyntaxType.TypeOfKeyword:
                case SyntaxType.VarKeyword:
                case SyntaxType.NewKeyword:
                case SyntaxType.UsingKeyword:
                    return ParseKeywordExpression(CurrentToken);
                case SyntaxType.OutKeyword:
                    return ParseOutExpressionSyntax();
                // Unary expressions...
                case SyntaxType.MinusToken:
                case SyntaxType.BackquoteToken:
                    return ParseUnaryExpressionSyntax();
                case SyntaxType.DollarToken:
                    return ParseStringInterpolationSyntax();
                default:
                    throw new Exception($"Syntax Error: Unexpected token {CurrentToken}");
            }
        }

        private OutExpressionSyntax ParseOutExpressionSyntax()
        {
            return new OutExpressionSyntax(m_SyntaxTree, ParseKeywordExpression(CurrentToken), ParseIdentifierName());
        }

        private UnaryExpressionSyntax ParseUnaryExpressionSyntax()
        {
            return new UnaryExpressionSyntax(m_SyntaxTree, EatToken(), ParseExpression());
        }

        private StringInterpolationExpressionSyntax ParseStringInterpolationSyntax()
        {
            return new StringInterpolationExpressionSyntax(m_SyntaxTree, EatToken(),
                ParseExpression() as LiteralExpressionSyntax);
        }

        private ExpressionSyntax ParsePostfixExpression(ExpressionSyntax expression)
        {
            while (true)
            {
                // Keyword Expression
                if (expression is KeywordExpressionSyntax kes)
                {
                    var type = kes.KeywordToken.Type;
                    switch (type)
                    {
                        case SyntaxType.TypeOfKeyword:
                            expression = new TypeOfExpressionSyntax(m_SyntaxTree, expression as KeywordExpressionSyntax,
                                ExpectToken(SyntaxType.OpenParenthesisToken),
                                ParseExpression(),
                                ExpectToken(SyntaxType.CloseParenthesisToken));
                            break;
                        case SyntaxType.VarKeyword:
                            expression = new DeclarationExpressionSyntax(m_SyntaxTree,
                                (KeywordExpressionSyntax) expression,
                                ParseExpression() as AssignmentExpressionSyntax);
                            break;
                        case SyntaxType.NewKeyword:
                            expression =
                                new ConstructorExpressionSyntax(m_SyntaxTree, kes,
                                    ParseExpression() as InvocationExpressionSyntax);
                            break;
                        case SyntaxType.UsingKeyword:
                            expression = new UsingExpressionSyntax(m_SyntaxTree, (KeywordExpressionSyntax) expression,
                                ParseExpression());
                            break;
                        default:
                            throw new NotSupportedException($"Keyword expression syntax not supported: {type}");
                    }
                }

                // Common Expressions
                switch (CurrentToken.Type)
                {
                    case SyntaxType.OpenParenthesisToken:
                        expression = new InvocationExpressionSyntax(m_SyntaxTree, expression,
                            ParseParenthesisedArgumentList());
                        break;
                    case SyntaxType.OpenAngleBracketToken:
                        expression = new GenericInvocationExpressionSyntax(m_SyntaxTree, expression,
                            ParseGenericArgumentListSyntax(), ParseParenthesisedArgumentList());
                        break;
                    case SyntaxType.OpenSquareBracketToken:
                        expression = new IndexAccessExpressionSyntax(m_SyntaxTree, expression, EatToken(),
                            ParseExpression(),
                            EatToken());
                        break;
                    case SyntaxType.DotToken:
                        expression = new MemberAccessExpressionSyntax(m_SyntaxTree, expression, EatToken(),
                            ParseIdentifierName());
                        break;
                    case SyntaxType.EqualsToken:
                        expression = new AssignmentExpressionSyntax(m_SyntaxTree, expression,
                            EatToken(), ParseBinaryExpression());
                        break;
                    default:
                        return expression;
                }
            }
        }

        private ArgumentSyntax ParseArgument()
        {
            return new ArgumentSyntax(m_SyntaxTree, ParseBinaryExpression());
        }

        private ParenthesisedArgumentListSyntax ParseParenthesisedArgumentList()
        {
            var openParenToken = ExpectToken(SyntaxType.OpenParenthesisToken);
            var nodesAndSeparators = new List<SyntaxNode>();
            var flag = true;
            while (flag && CurrentToken.Type != SyntaxType.CloseParenthesisToken &&
                   CurrentToken.Type != SyntaxType.EndOfFile)
            {
                var expression = ParseArgument();
                nodesAndSeparators.Add(expression);
                // Encountered with comma or whitespace, keep going...
                if (CurrentToken.Type == SyntaxType.CommaToken)
                {
                    nodesAndSeparators.Add(ExpectToken(SyntaxType.CommaToken));
                }
                else
                {
                    flag = false;
                }
            }

            var closeParenToken = ExpectToken(SyntaxType.CloseParenthesisToken);
            return new ParenthesisedArgumentListSyntax(m_SyntaxTree, openParenToken,
                new SeparatedSyntaxList<ArgumentSyntax>(nodesAndSeparators),
                closeParenToken);
        }

        private GenericArgumentListSyntax ParseGenericArgumentListSyntax()
        {
            var openParenToken = ExpectToken(SyntaxType.OpenAngleBracketToken);
            var nodesAndSeparators = new List<SyntaxNode>();
            var flag = true;
            while (flag && CurrentToken.Type != SyntaxType.CloseAngleBracketToken &&
                   CurrentToken.Type != SyntaxType.EndOfFile)
            {
                var expression = ParseArgument();
                nodesAndSeparators.Add(expression);
                // Encountered with comma or whitespace, keep going...
                if (CurrentToken.Type == SyntaxType.CommaToken)
                {
                    nodesAndSeparators.Add(ExpectToken(SyntaxType.CommaToken));
                }
                else
                {
                    flag = false;
                }
            }

            var closeParenToken = ExpectToken(SyntaxType.CloseAngleBracketToken);
            return new GenericArgumentListSyntax(m_SyntaxTree, openParenToken,
                new SeparatedSyntaxList<ArgumentSyntax>(nodesAndSeparators),
                closeParenToken);
        }

        #endregion

        #region Field

        private readonly SyntaxTree m_SyntaxTree;

        private SyntaxToken CurrentToken => PeekToken(0);

        private int m_Position;

        #endregion
    }
}