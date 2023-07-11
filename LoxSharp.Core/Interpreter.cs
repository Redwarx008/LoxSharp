using System.Diagnostics;

namespace LoxSharp.Core
{
    public class Interpreter
    {
        private Scanner _scanner;
        private Compiler _compiler;
        private VM _vm;
        public Interpreter()
        {
            _scanner = new Scanner();
            _compiler = new Compiler();
            _vm = new VM();
        }

        public void Run(string src)
        {
            _scanner.Reset();
            _compiler.Reset();
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Token> tokens = _scanner.Scan(src);
            stopwatch.Stop();
            var compiledScript = _compiler.Compile(tokens);

            _vm.Interpret(compiledScript);

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }

    }
}
