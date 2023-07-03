using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    using ParseFunc = Action;
    internal class Compiler
    {
        private enum Precedence
        {
            None,
            Assignment, // =
            Or,         // or
            And,        // and
            Equality,   // == !=
            Comparison, // < > <= >=
            Term,       // + -
            Factor,     // * /
            Unary,      // ! -
            Call,       // . ()
            Primary,
        }

        private class ParseRule
        {
            public Action? Prefix { get; private set; } = null;
            public Action? Infix { get; private set; } = null;
            public Precedence Precedence { get; private set; } = Precedence.None;

            public ParseRule(Action? prefix, Action? infix, Precedence precedence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precedence;
            }
        }

        private readonly ParseRule[] _rules;

        private Token _previous;
        private Token _current;
        private int _tokenIndex;
        private List<Token>? _tokens;

        private Chunk? _compilingChunk;

        public Chunk CurrentChunk => _compilingChunk!;
        public Compiler()
        {
            _rules = new ParseRule[Enum.GetValues(typeof(TokenType)).Length];

            _rules[(int)TokenType.LEFT_PAREN] = new ParseRule(Grouping, null, Precedence.None);
            _rules[(int)TokenType.RIGHT_PAREN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LEFT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RIGHT_BRACE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.COMMA] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.DOT] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.MINUS] = new ParseRule(Unary, Binary, Precedence.Term);
            _rules[(int)TokenType.PLUS] = new ParseRule(null, Binary, Precedence.Term);
            _rules[(int)TokenType.SEMICOLON] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SLASH] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.STAR] = new ParseRule(null, Binary, Precedence.Factor);
            _rules[(int)TokenType.BANG] = new ParseRule(Unary, null, Precedence.None);
            _rules[(int)TokenType.BANG_EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EQUAL_EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.GREATER] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.GREATER_EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LESS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.LESS_EQUAL] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.IDENTIFIER] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.STRING] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.NUMBER] = new ParseRule(Number, null, Precedence.None);
            _rules[(int)TokenType.AND] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.CLASS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.ELSE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FALSE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.FOR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.FUN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.IF] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.NIL] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.OR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.PRINT] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.RETURN] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.SUPER] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.THIS] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.TRUE] = new ParseRule(Literal, null, Precedence.None);
            _rules[(int)TokenType.VAR] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.WHILE] = new ParseRule(null, null, Precedence.None);
            _rules[(int)TokenType.EOF] = new ParseRule(null, null, Precedence.None);
        }

        public Chunk Compile(List<Token> tokens)
        {
            _tokens = tokens;
            Chunk chunk = new Chunk();  
            _compilingChunk = chunk;    
            Advance();
            while(_current.Type != TokenType.EOF)
            {
                Expression();
            }
            return chunk;   
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private void Advance()
        {
            Debug.Assert(_tokens != null);

            _previous = _current;
            _current = _tokens[_tokenIndex];
            ++_tokenIndex;
        }

        [method:MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Consume(TokenType type, string message)
        {
            if(_current.Type == type)
            {
                Advance();
            }
            else
            {
                throw new CompilerException(_current, message);
            }
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Check(TokenType type)
        {
            return _current.Type == type;   
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(TokenType type)
        {
            if(!Check(type))
            {
                return false;
            }
            Advance();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitByte(byte b)
        {
            CurrentChunk.WriteByte(b, _previous.Line);  
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitBytes(byte b1, byte b2)
        {
            EmitByte(b1);
            EmitByte(b2);   
        }

        private void EmitReturn()
        {
            EmitByte((byte)OpCode.RETURN);
        }

        [MethodImpl(methodImplOptions:MethodImplOptions.AggressiveInlining)]    
        private void EmitConstant(Value val)
        {
            EmitBytes((byte)OpCode.CONSTANT, MakeConstant(val));
        }

        private byte MakeConstant(Value val)
        {
            int index = CurrentChunk.AddConstant(val);
            if(index > byte.MaxValue)
            {
                throw new CompilerException(_previous, "Too many constants in one chunk.");
            }
            return (byte)index;
        }

        private void EndCompiler()
        {
            EmitReturn();
        }

        private void Literal()
        {
            switch(_previous.Type)
            {
                case TokenType.FALSE:
                    EmitByte((byte)OpCode.FALSE);
                    break;
                case TokenType.TRUE:
                    EmitByte((byte)OpCode.TRUE);
                    break;
                case TokenType.NIL:
                    EmitByte((byte)OpCode.NIL);
                    break;
                default: return; // Unreachable.
            }
        }
        private void Number()
        {
            double value = (double)_previous.Literal!;
            EmitConstant(new Value(value));
        }

        private void Unary()
        {
            TokenType operatorType = _previous.Type;

            // Compile the operand.
            ParsePrecedence(Precedence.Unary);

            switch (operatorType) 
            {
                case TokenType.BANG:
                    EmitByte((byte)OpCode.NOT);
                    break;
                case TokenType.MINUS:
                    EmitByte((byte)OpCode.NEGATE);
                    break;
                default:
                    return;// Unreachable.
            }
        }

        private void Binary()
        {
            TokenType operatorType = _previous.Type;
            ParseRule rule = GetRule(operatorType);
            ParsePrecedence(rule.Precedence + 1);

            switch (operatorType) 
            {
                case TokenType.PLUS:
                    EmitByte((byte)OpCode.ADD);
                    break;
                case TokenType.MINUS:
                    EmitByte((byte)OpCode.SUBTRACT);
                    break;
                case TokenType.STAR:
                    EmitByte((byte)OpCode.MULTIPLY);
                    break;
                case TokenType.SLASH: 
                    EmitByte((byte)OpCode.DIVIDE);
                    break;
                default: return; // Unreachable.
            }
        }

        private void Grouping()
        {
            Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
        }

        /// <summary>
        /// We simply parse the lowest precedence level, 
        /// which subsumes all of the higher-precedence expressions too. 
        /// </summary>
        private void Expression()
        {
            ParsePrecedence(Precedence.Assignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ParseRule GetRule(TokenType type)
        {
            return _rules[(int)type];
        }

        private void ParsePrecedence(Precedence precedence)
        {
            Advance();

            ParseFunc? prefixRule = GetRule(_previous.Type).Prefix;
            if (prefixRule == null)
            {
                throw new CompilerException(_previous, "Expect expression.");
            }

            prefixRule();

            while(precedence <= GetRule(_current.Type).Precedence) 
            {
                Advance();
                ParseFunc infixRule = GetRule(_previous.Type).Infix!;
                infixRule();
            }
        }
    }
}
