using System;
using System.Collections.Generic;
using System.Linq;
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

        static Compiler()
        {
            _rules = new ParseRule[Enum.GetValues(typeof(ParseRule)).Length];

        }

    }
}
