using LoxSharp.Core.Utility;

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
            return $"<<{Name}>>";
        }
    }
}
