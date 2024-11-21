namespace Feval.Syntax
{
    public enum SyntaxType
    {
        #region Token

        Invalid,
        WhitespaceToken,
        PlusToken,
        MinusToken,
        MultiplyToken,
        DivideToken,
        CommaToken,
        SemicolonToken,
        DotToken,
        DollarToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenAngleBracketToken,
        CloseAngleBracketToken,
        OpenSquareBracketToken,
        CloseSquareBracketToken,
        EqualsToken,
        EqualsEqualsToken,
        IdentifierToken,
        BackquoteToken,
        PipeToken,

        #endregion

        #region Literal

        StringLiteral,
        IntLiteral,
        FloatLiteral,
        LongLiteral,

        #endregion

        #region Keyword

        NewKeyword,
        TrueKeyword,
        FalseKeyword,
        TypeOfKeyword,
        VarKeyword,
        NullKeyword,
        UsingKeyword,
        OutKeyword,

        #endregion

        #region Expression

        Argument,
        ArgumentList,
        GenericArgumentList,
        IdentifierName,
        InvocationExpression,
        ConstructorExpression,
        GenericInvocationExpression,
        IndexAccessExpression,
        UnaryExpression,
        BinaryExpression,
        MemberAccessExpression,
        AssignmentExpression,
        TypeOfExpression,
        KeywordExpression,
        DeclarationExpression,
        UsingExpression,
        OutExpression,
        StringInterpolationExpression,

        #endregion

        EndOfFile
    }
}