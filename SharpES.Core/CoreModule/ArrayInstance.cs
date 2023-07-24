namespace SharpES.Core
{
    internal class ArrayInstance : ClassInstance
    {
        public List<Value> Values { get; private set; }
        public ArrayInstance(Array array)
            : base(array)
        {
            Values = new List<Value>();
        }
    }
}
