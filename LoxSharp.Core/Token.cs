namespace LoxSharp.Core
{
    internal enum TokenType
    {
        // Single-character tokens.
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE, COLON,
        COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR, PERCENT,
        LEFT_BRACKET, RIGHT_BRACKET, 
        // One or two character tokens.
        BANG, BANG_EQUAL,
        EQUAL, EQUAL_EQUAL,
        GREATER, GREATER_EQUAL,
        LESS, LESS_EQUAL,

        // Literals.
        IDENTIFIER, STRING, NUMBER,

        // Keywords.
        AND, CLASS, ELSE, FALSE, FUN, FOR, IF, NULL,
        OR, RETURN, SUPER, THIS, TRUE, VAR, WHILE,
        CONTINUE, BREAK, IMPORT, AS, FROM, STATIC,

        ERROR, EOF
    }
    internal readonly struct Token
    {
        public readonly TokenType Type;
        public readonly string Lexeme;
        public readonly int Line;

        public Token(TokenType type, string lexeme, int line)
        {
            this.Type = type;
            this.Lexeme = lexeme;
            this.Line = line;
        }

        public override string ToString()
        {
            return $"Line {Line} {Type} {Lexeme}";
        }
    }
}
