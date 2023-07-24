namespace SharpES.Core
{
    public delegate Value ForeignMethodDelegate(ClassInstance instance, Value[] args);
    public class ForeignMethod
    {
        public string Name { get; private set; }

        public ForeignMethodDelegate Method { get; private set; }
        public ForeignMethod(string name, ForeignMethodDelegate method)
        {
            Name = name;
            Method = method;
        }

        public override string ToString()
        {
            return $"<Name>";
        }
    }
}
