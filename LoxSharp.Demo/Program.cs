using LoxSharp.Core;

namespace LoxSharp.Demo
{
    internal class Program
    {
        private static ScriptEngine engine = ScriptEngine.GetInstance;
        static void Main(string[] args)
        {

            if (args.Length > 1)
            {
                Console.WriteLine("Usage: jlox [script]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
            }
        }
        private static void RunFile(string path)
        {
            string data = File.ReadAllText(path);
            Run(data);
        }
        private static void RunPrompt()
        {
            while (true)
            {
                Console.Write(">");
                string? line = Console.ReadLine();
                if (line == null)
                    break;
                Run(line);
            }
        }
        private static void Run(string source)
        {
            ScriptConfiguration configuration = new ScriptConfiguration()
            {
                PrintErrorFn = PrintError,
                WriteFunction = Console.Write
            };
            VM vM = new VM(configuration);
            engine.Run(vM, source);
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