using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public delegate string LoadMuduleFn(string moduleName);

    public delegate void WriteFn(string message);
    public class ScriptConfiguration
    {
        public WriteFn? WriteFunction { get; set; }
        public LoadMuduleFn? LoadMuduleFunction { get; set; }
        public ScriptConfiguration() 
        {

        }
    }
}
