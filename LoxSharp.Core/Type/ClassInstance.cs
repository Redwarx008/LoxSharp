namespace LoxSharp.Core
{
    public class ClassInstance
    {
        public Class Class { get; private set; }

        public Dictionary<string, Value> Fields { get; private set; }

        internal ClassInstance(Class @class)
        {
            Fields = new Dictionary<string, Value>();
            Class = @class;
        }

        public override string ToString()
        {
            return $"{Class} instance";
        }
    }
}
