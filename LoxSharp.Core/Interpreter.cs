using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public class Interpreter
    {
        private Scanner _scanner;

        private static StringBuilder _errorSB;

        private bool _hadError = false; 

        static Interpreter()
        {
            _errorSB = new StringBuilder();
        }
        public Interpreter() 
        {
            _scanner = new Scanner();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CompileError(Token token, string message)
        {
            _errorSB.AppendLine($"[line {token.Line}] Error at {token.Name} | {message}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ScanError(int line, string message)
        {
            _errorSB.AppendLine($"[line {line}] | {message}");
        }
    }
}
