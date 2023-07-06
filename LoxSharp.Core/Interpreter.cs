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
        private Disassembler _disassembler;
        public Interpreter() 
        {
            _scanner = new Scanner();
            _compiler = new Compiler();
            _vm = new VM();
            _disassembler = new Disassembler();
        }

        public void Run(string src)
        {
            _scanner.Reset();
            _compiler.Reset();
            List<Token> tokens = _scanner.Scan(src);    
            Chunk chunk = _compiler.Compile(tokens);    
            try
            {
                _vm.Interpret(chunk);
            }
            catch(RuntimeException e) 
            {
                Console.WriteLine(e.ToString());    
            }
            catch (Exception e) { }
            _disassembler.DisassembleChunk(chunk, "instructions");
            Console.Write(_disassembler.GetText()); 
        }

    }
}
