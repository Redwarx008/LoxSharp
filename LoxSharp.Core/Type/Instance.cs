namespace LoxSharp.Core
{
    internal class Instance
    {
        public InternalClass Class { get; private set; }

        public Dictionary<string, Value> Fields { get; private set; }

        public Instance(InternalClass internalClass)
        {
            Fields = new Dictionary<string, Value>();
            Class = internalClass;
        }

        public override string ToString()
        {
            return $"{Class} instance";
        }
    }
}
