namespace CompilerLabs.Core.Lexer
{
    public enum TokenType
    {
        NUMBER,
        ID,
        STRING,
        TRUE,
        FALSE,
        VAR,
        FUNC,
        RETURN,

        PRINT,
        IF, ELSE,
        WHILE,

        PLUS, MINUS, STAR, SLASH,
        EQ, EQEQ, EXCL, NEQ,
        LT, GT, LTEQ, GTEQ,
        AND, OR,

        LPAREN, RPAREN,
        LBRACE, RBRACE,
        LBRACKET, RBRACKET,
        COMMA,
        SEMICOLON,

        EOF
    }
}