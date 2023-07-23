using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public delegate Value ForeignFunctionDelegate(Value[] args);

    /// <summary>
    ///  Used as foreign module function and foreign class static methods.
    /// </summary>
    internal class ForeignFunction
    {
        public string Name { get; private set; }    
        public ForeignFunctionDelegate Function { get; private set; }
        public ForeignFunction(string name, ForeignFunctionDelegate functionDelegate) 
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
