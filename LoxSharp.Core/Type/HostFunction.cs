using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public delegate Value HostFunctionDelegate(Value[] args);

    internal class HostFunction
    {
        public string Name { get; private set; }    
        public HostFunctionDelegate Function { get; private set; }
        public HostFunction(string name, HostFunctionDelegate functionDelegate) 
        {
            Name = name;
            Function = functionDelegate;
        }   

        public override string ToString() 
        {
            return $"<Fn {Name}>";
        }
    }
}
