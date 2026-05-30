namespace CompilerLabs.Core.Lexer
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Position { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int position, int line, int column)
        {
            Type = type;
            Value = value;
            Position = position;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"[{Line}:{Column}] Token({Type}, '{Value}')";
        }
    }
}
