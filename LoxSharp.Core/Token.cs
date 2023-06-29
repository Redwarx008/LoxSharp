using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal enum TokenType
    {
        // Single-character tokens.
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
        COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,

        // One or two character tokens.
        BANG, BANG_EQUAL,
        EQUAL, EQUAL_EQUAL,
        GREATER, GREATER_EQUAL,
        LESS, LESS_EQUAL,

        // Literals.
        IDENTIFIER, STRING, NUMBER,

        // Keywords.
        AND, CLASS, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
        PRINT, RETURN, SUPER, THIS, TRUE, VAR, WHILE,

        EOF
    }
    internal readonly struct Token
    {
        public readonly TokenType Type;
        public readonly string Name;
        public readonly object? Literal;
        public readonly int Line;

        public Token(TokenType type, string name, object? literal, int line)
        {
            this.Type = type;
            this.Name = name;
            this.Literal = literal;
            this.Line = line;
        }

        public override string ToString() 
        {
            return $"Line {Line} {Type} {Name}";
        }
    }
}
