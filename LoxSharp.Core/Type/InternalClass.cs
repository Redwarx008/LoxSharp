namespace LoxSharp.Core
{
    public class InternalClass
    {
        public string Name { get; private set; }

        public Dictionary<string, Value> Methods { get; private set; }

        public InternalClass(string name)
        {
            Name = name;
            Methods = new Dictionary<string, Value>();
        }

        public override string ToString()
        {
            return $"<<{Name}>>";
        }
    }
}
