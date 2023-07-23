using System.Runtime.CompilerServices;

namespace LoxSharp.Core
{
    internal class Scanner
    {
        private string _source;

        private static Dictionary<string, TokenType> _keyWords;

        private int _start = 0;
        private int _current = 0;
        private int _line = 1;

        static Scanner()
        {
            _keyWords = new Dictionary<string, TokenType>();
            _keyWords["and"] = TokenType.AND;
            _keyWords["class"] = TokenType.CLASS;
            _keyWords["else"] = TokenType.ELSE;
            _keyWords["false"] = TokenType.FALSE;
            _keyWords["for"] = TokenType.FOR;
            _keyWords["fun"] = TokenType.FUN;
            _keyWords["if"] = TokenType.IF;
            _keyWords["nil"] = TokenType.NULL;
            _keyWords["or"] = TokenType.OR;
            _keyWords["return"] = TokenType.RETURN;
            _keyWords["super"] = TokenType.SUPER;
            _keyWords["this"] = TokenType.THIS;
            _keyWords["true"] = TokenType.TRUE;
            _keyWords["var"] = TokenType.VAR;
            _keyWords["while"] = TokenType.WHILE;
            _keyWords["continue"] = TokenType.CONTINUE;
            _keyWords["break"] = TokenType.BREAK;
            _keyWords["import"] = TokenType.IMPORT;
            _keyWords["as"] = TokenType.AS;
            _keyWords["from"] = TokenType.FROM;
            _keyWords["static"] = TokenType.STATIC;
        }

        public Scanner(string source)
        {
            _source = source;
        }
        public  List<Token> ScanSource()
        {
            var tokens = new List<Token>();
            while (!IsAtEnd())
            {
                tokens.Add(ScanToken());
            }
            Reset();
            return tokens;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            _line = 1;
            _current = 0;
            _start = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token ScanToken()
        {
            SkipWhitespace();

            // We are at the beginning of the next lexeme.
            _start = _current;
            if (IsAtEnd()) 
            { 
                return MakeToken(TokenType.EOF);
            }

            char c = Advance();
            switch (c)
            {
                case '(': return MakeToken(TokenType.LEFT_PAREN);
                case ')': return MakeToken(TokenType.RIGHT_PAREN);
                case '{': return MakeToken(TokenType.LEFT_BRACE);
                case '}': return MakeToken(TokenType.RIGHT_BRACE);
                case '[': return MakeToken(TokenType.LEFT_BRACKET);
                case ']': return MakeToken(TokenType.RIGHT_BRACKET);
                case ',': return MakeToken(TokenType.COMMA);
                case '.': return MakeToken(TokenType.DOT);
                case '-': return MakeToken(TokenType.MINUS);
                case '+': return MakeToken(TokenType.PLUS);
                case ';': return MakeToken(TokenType.SEMICOLON);
                case '*': return MakeToken(TokenType.STAR);
                case '/': return MakeToken(TokenType.SLASH);
                case '%': return MakeToken(TokenType.PERCENT);
                case '!':
                    return MakeToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                case '=':
                    return MakeToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                case '<':
                    return MakeToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                case '>':
                    return MakeToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                case '"':
                    return MakeString();
                default:
                    if (IsDigit(c))
                    {
                        return MakeNumber();
                    }
                    else if (IsAlpha(c))
                    {
                        return MakeIdentifier();
                    }
                    else
                    {
                        throw new ScannerException(_line, "Unexpected character.");
                    }
            }
        }

        private void SkipWhitespace()
        {
            while (true) 
            {
                char c = Peek();
                switch(c)
                {
                    case '/':
                        if (Match('/'))
                        {
                            // A comment goes until the end of the line.
                            while (!IsAtEnd() && _source![_current] != '\n')
                            {
                                //Advance();
                                ++_current;
                            }
                        }
                        else
                        {
                            return;
                        }
                        break;
                    case ' ':
                    case '\r':
                    case '\t':
                        ++_current;
                        break;
                    case '\n':
                        ++_line;
                        ++_current;
                        break;
                    default:
                        return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Token MakeIdentifier()
        {
            while (IsAlphaNumeric(Peek()))
            {
                ++_current;
            }

            string text = _source!.Substring(_start, _current - _start);
            TokenType type = TokenType.IDENTIFIER;
            if (!_keyWords.TryGetValue(text, out type))
            {
                type = TokenType.IDENTIFIER;
            }
            return MakeToken(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Token MakeNumber()
        {
            while (IsDigit(Peek()))
            {
                ++_current;
            }
            // Look for a fractional part.
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consume the "."
                ++_current;
            }
            while (IsDigit(Peek()))
            {
                ++_current;
            }
            return MakeToken(TokenType.NUMBER);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Token MakeString()
        {
            while (!IsAtEnd() && _source![_current] != '"')
            {
                if (_source[_current] == '\n')
                {
                    ++_line;
                }
                ++_current;
            }
            if (IsAtEnd())
            {
                throw new ScannerException(_line, "Unterminated string.");
            }
            // The closing ".
            ++_current;
            // Trim the surrounding quotes.
            string str = _source!.Substring(_start + 1, _current - 1 - (_start + 1));
            return new Token(TokenType.STRING, str, _line);  
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAtEnd()
        {
            return _current >= _source!.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Advance()
        {
            _current++;
            return _source![_current - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Token MakeToken(TokenType type)
        {
            string name = _source!.Substring(_start, _current - _start);
            return new Token(type, name, _line);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(char expected)
        {
            if (IsAtEnd())
            {
                return false;
            }
            if (_source![_current] != expected)
            {
                return false;
            }
            _current++;
            return true;
        }

        /// <summary>
        /// It’s sort of like advance(), but doesn’t consume the character.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Peek()
        {
            if (IsAtEnd())
            {
                return '\0';
            }
            return _source![_current];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char PeekNext()
        {
            if (_current + 1 >= _source!.Length)
            {
                return '\0';
            }
            return _source[_current + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAlphaNumeric(char c)
        {
            return IsDigit(c) || IsAlpha(c);
        }
    }
}
