using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class ArrayInstance : ClassInstance
    {
        public List<Value> Values { get; private set; } 
        public ArrayInstance(Array array)
            :base(array)
        {
            Values = new List<Value>(); 
        }
    }
}
