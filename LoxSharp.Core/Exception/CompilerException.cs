namespace LoxSharp.Core
{
    public class CompilerException : Exception
    {
        internal CompilerException(Token token, string message)
            : base($"Compile error : [line {token.Line}] Error at '{token.Name}' {message}")
        { }
    }
}
