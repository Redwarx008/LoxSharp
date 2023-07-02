using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
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
            public Action<bool> Prefix { get; private set; }
            public Action<bool> Infix { get; private set; }
            public Precedence precedence { get; private set; }

            public ParseRule(Action<bool> prefix, Action<bool> infix, Precedence precedence)
            {
                Prefix = prefix;
                Infix = infix;
                this.precedence = precedence;
            }
        }

        private static ParseRule[] _rules;

        private Token _previous;
        private Token _current;
        private int _tokenIndex;
        private List<Token>? _tokens;

        private Chunk? _compilingChunk;

        public Chunk CurrentChunk => _compilingChunk!;
        static Compiler()
        {
            _rules = new ParseRule[Enum.GetValues(typeof(ParseRule)).Length];

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

        private void EmitByte(byte b)
        {
            CurrentChunk.WriteByte(b, _previous.Line);  
        }

        private void EmitBytes(byte b1, byte b2)
        {
            EmitByte(b1);
            EmitByte(b2);   
        }
        private void EmitReturn()
        {
            EmitByte((byte)OpCode.RETURN);
        }

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

        private void Expression()
        {

        }
    }
}
