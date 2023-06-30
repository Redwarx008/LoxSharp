using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public class ScannerException : Exception
    {
        public ScannerException(int line, string message) 
            :base($"[line {line}] | {message}")
        {

        }
    }
}
