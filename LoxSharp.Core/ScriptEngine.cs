using System;
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
            if(_globalValuesIndexs.ContainsKey(name))
            {
                throw new ExtensionException($"A Global variable named {name} already exists.");
            }
            else
            {
                int index = _globalValues.Count;
                _globalValuesIndexs[name] = index;  
                _globalValues.Add(value);
            }
        }

        public Value GetGlobal(string name)
        {
            if (!_globalValuesIndexs.TryGetValue(name, out int index))
            {
                //  If it does not exist, return null.
                return new Value(); 
            }

            return _globalValues[index];
        }

        public void CallFunction(Value func, params Value[] args)
        {
            VM vm = new(_globalValues);
            vm.CallFunction(func, args);
        }

        public void CallFunction(string name, params Value[] args) 
        {
            if (!_globalValuesIndexs.TryGetValue(name, out int index))
            {
                throw new ExtensionException($"can't find function named {name}.");
            }

            VM vm = new(_globalValues);
            vm.CallFunction(_globalValues[index], args);
        }

        public void AddGlobalFunction(string name, HostFunctionDelegate hostFunction)
        {
            HostFunction function = new(name, hostFunction);
            SetGlobal(name, new Value(function));
        }

    }
}
