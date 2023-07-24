namespace SharpES.Core
{
    public enum ErrorType
    {
        CompileError,
        RuntimeError,
        OtherError
    }
    /// <summary>
    /// Loads and returns the source code for the module.
    /// </summary>
    /// <returns>source code</returns>
    public delegate string LoadModuleFn(string moduleName);

    /// <summary>
    /// Displays a string of text to the user.
    /// </summary>
    public delegate void WriteFn(string message);

    /// <summary>
    /// Reports an error to the user.
    /// </summary>
    public delegate void PrintErrorFn(ErrorType errorType, string moduleName, int line, string message);

    public class ScriptConfiguration
    {
        public WriteFn? WriteFunction { get; set; }
        public LoadModuleFn? LoadModuleFunction { get; set; }
        public PrintErrorFn? PrintErrorFn { get; set; }
        public ScriptConfiguration()
        {

        }
    }
}
