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
        private Compiler _compiler;
        private VM _vm;

        private bool _hadError = false; 

        public Interpreter() 
        {
            _scanner = new Scanner();
            _compiler = new Compiler();
            _vm = new VM();
        }

        public void Run(string src)
        {
            _scanner.Reset();
            List<Token> tokens = _scanner.Scan(src);    
            Chunk chunk = _compiler.Compile(tokens);    
            _vm.Interpret(chunk);
        }

    }
}
