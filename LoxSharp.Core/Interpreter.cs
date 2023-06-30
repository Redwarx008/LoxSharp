using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public class Interpreter
    {
        private Scanner _scanner;

        private bool _hadError = false; 

        public Interpreter() 
        {
            _scanner = new Scanner();
        }


    }
}
