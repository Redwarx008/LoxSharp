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
            Methods["init"] = new Value(new HostMethod("init", Init));
            Methods[nameof(Count)] = new Value(new HostMethod(nameof(Count), Count));
            Methods[nameof(Add)] = new Value(new HostMethod(nameof(Add), Add));
        }

        public override ClassInstance CreateInstance() => new ArrayInstance(this);

        private Value Init(ClassInstance instance, Value[] args)
        {
            List<Value> list = ((ArrayInstance)instance).Values;
            list.AddRange(args);
            // Constructor should return an instance.
            return new Value(instance);
        }

        private Value Count(ClassInstance instance, Value[] args)
        {
            return new Value(((ArrayInstance)instance).Values.Count);
        }

        private Value Add(ClassInstance instance, Value[] args)
        {
            ((ArrayInstance)instance).Values.Add(args[0]);
            return Value.Null;
        }
    }
}
