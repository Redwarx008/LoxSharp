using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class MapInstance : ClassInstance
    {
        public Dictionary<Value, Value> Entries { get; private set; }
        public MapInstance(Map map)
            :base(map)
        {
            Entries = new Dictionary<Value, Value>();
        }
    }
}
