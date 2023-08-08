using SharpES.Core;
using System;
using System.IO;

namespace SharpES.Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {

            if (args.Length > 1)
            {
                Console.WriteLine("Usage: [script]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
        }
        private static void RunFile(string path)
        {
            string data = File.ReadAllText(path);
            Run(data);
        }

        private static void Run(string source)
        {
            ScriptConfiguration configuration = new ScriptConfiguration()
            {
                PrintErrorFn = PrintError,
                WriteFunction = Console.Write,
                LoadModuleFunction = LoadModule
            };
            VM vM = new VM(configuration);
            //ForeignFunction logFunc = new("log", (args) =>
            //{
            //    if (args.Count > 1)
            //    {
            //        // error handle.
            //    }
            //    Console.WriteLine(args[0]);
            //    return Value.NUll;  // must return a value, which is consistent with the script's behavior.
            //});
            //ScriptEngine.RegisterForeignFunction(vM, logFunc);

            //Value? callee = ScriptEngine.GetModuleVariable(vM, string.Empty, "log");
            //if (callee != null) 
            //{
            //    ScriptEngine.Call(vM, callee.Value, new Value("hello world"));
            //}
            InterpretResult res = ScriptEngine.Run(vM, source);

        }

        private static string LoadModule(string moduleName)
        {
            return File.ReadAllText(moduleName + ".lox");
        }

        private static void PrintError(ErrorType errorType, string moduleName, int line, string message)
        {
            switch (errorType)
            {
                case ErrorType.CompileError:
                    Console.WriteLine($"Compile error : [{moduleName} line {line}] {message}");
                    break;
                case ErrorType.RuntimeError:
                    Console.WriteLine($"Runtime error : [{moduleName} line {line}] {message}");
                    break;
            }
        }
    }
}