using LoxSharp.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class BoundMethod
    {
        public Value Receiver { get; private set; } 
        public Function Function { get; private set; }

        public BoundMethod(Value receiver, Function function)
        {
            Receiver = receiver;
            Function = function;
        }

        public override string ToString() 
        {
            return Function.ToString(); 
        }
    }
}
