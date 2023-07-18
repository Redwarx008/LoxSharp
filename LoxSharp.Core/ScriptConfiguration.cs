using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public enum ErrorType
    {
        CompileError,
        RuntimeError,
    }
    /// <summary>
    /// Loads and returns the source code for the module.
    /// </summary>
    /// <returns>source code</returns>
    public delegate string LoadMuduleFn(string moduleName);

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
        public LoadMuduleFn? LoadMuduleFunction { get; set; }
        public PrintErrorFn? PrintErrorFn { get; set; }
        public ScriptConfiguration() 
        {

        }
    }
}
