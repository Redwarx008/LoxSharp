using System.Collections.Generic;

namespace SharpES.Core
{
    internal class MapInstance : ClassInstance
    {
        public Dictionary<Value, Value> Entries { get; private set; }
        public MapInstance(Map map)
            : base(map)
        {
            Entries = new Dictionary<Value, Value>();
        }
    }
}
