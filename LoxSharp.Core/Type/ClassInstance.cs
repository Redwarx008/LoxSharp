namespace LoxSharp.Core
{
    public class ClassInstance
    {
        public InternalClass Class { get; private set; }

        public Dictionary<string, Value> Fields { get; private set; }

        internal ClassInstance(InternalClass internalClass)
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
