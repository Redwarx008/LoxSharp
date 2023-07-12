using System.Diagnostics;

namespace LoxSharp.Core
{
    public class ScriptEngine
    {
        private readonly List<Value> _globalValues;
        private readonly Dictionary<string, int> _globalValuesIndexs;

        private readonly Compiler _compiler;
        private readonly Scanner _scanner;
        public ScriptEngine()
        {
            _globalValues = new();
            _globalValuesIndexs = new();

            _scanner = new();
            _compiler = new(_globalValues, _globalValuesIndexs);
        }

        public void Run(string src)
        { 
            Stopwatch stopwatch = new();

            VM vm = new(_globalValues);
            stopwatch.Start();
            var tokens = _scanner.Scan(src);

            var compiledScript = _compiler.Compile(tokens);
            stopwatch.Stop();


            vm.Interpret(compiledScript);

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            
        }

        public void SetGlobal(string name, Value value)
        {

        }

    }
}
