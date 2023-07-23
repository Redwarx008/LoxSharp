using LoxSharp.Core;
using System.Diagnostics;

namespace LoxSharp.Demo
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
            ScriptEngine.Run(vM, source);
            var b = ScriptEngine.GetModuleVariable(vM, "b");
            var res = ScriptEngine.Call(vM, b.Value);
            Console.WriteLine(res?.AsString ?? null);
        }

        private static string LoadModule(string moduleName)
        {
            return  File.ReadAllText(moduleName + ".lox");
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