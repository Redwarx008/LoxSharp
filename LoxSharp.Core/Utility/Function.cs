using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core.Utility
{
    internal class Function
    {
        public int Arity { get; private set; }
        public Chunk Chunk { get; private set; }
        public string? Name { get; private set; }

        public Function() 
        {
            Chunk = new Chunk();
            Arity = 0;  
        }
        public override string ToString()
        {
            if(Name == null)
            {
                return "<Main>";
            }

            return $"<Function {Name}>";
        }
    }
}
