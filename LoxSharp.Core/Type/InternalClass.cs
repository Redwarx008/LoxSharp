using LoxSharp.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class InternalClass
    {
        public string Name { get; private set; } 

        public Dictionary<string, Function> Methods { get; private set; }
        
        public InternalClass(string name)
        {
            Name = name;
            Methods = new Dictionary<string, Function>();
        }

        public override string ToString() 
        {
            return $"class {Name}";
        }
    }
}
