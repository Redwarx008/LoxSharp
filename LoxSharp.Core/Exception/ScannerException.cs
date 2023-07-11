namespace LoxSharp.Core
{
    public class ScannerException : Exception
    {
        public ScannerException(int line, string message)
            : base($"Scan error : [line {line}] | {message}")
        {

        }
    }
}
