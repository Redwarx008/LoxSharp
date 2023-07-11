using LoxSharp.Core;

namespace LoxSharp.Demo
{
    internal class Program
    {
        private static Interpreter interpreter = new Interpreter();
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
        private static void Run(String source)
        {
            try
            {
                interpreter.Run(source);
            }
            catch (ScannerException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (RuntimeException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}