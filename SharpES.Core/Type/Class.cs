namespace SharpES.Core
{
    public class Class
    {
        public string Name { get; private set; }

        public Dictionary<string, Value> Methods { get; private set; }
        public Dictionary<string, Value> StaticMethod { get; private set; }
        public Class(string name)
        {
            Name = name;
            Methods = new Dictionary<string, Value>();
            StaticMethod = new Dictionary<string, Value>();
        }

        public virtual ClassInstance CreateInstance() => new ClassInstance(this);

        public override string ToString()
        {
            return $"<<{Name}>>";
        }
    }
}
