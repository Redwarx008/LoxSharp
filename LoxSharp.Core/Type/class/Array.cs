using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class Array : InternalClass
    {
        public Array()
            :base("Array")
        {
            Methods[nameof(Count)] = new Value(new HostMethod(nameof(Count), Count));
        }

        private Value Count(ClassInstance instance, Value[] args)
        {
            return new Value(instance.Values.Count);
        }
    }
}
