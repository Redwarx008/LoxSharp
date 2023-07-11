namespace LoxSharp.Core.Utility
{
    internal class Function
    {
        public int Arity { get; set; }
        public Chunk Chunk { get; private set; }
        public string? Name { get; set; }

        public Function()
        {
            Chunk = new Chunk();
            Arity = 0;
        }
        public override string ToString()
        {
            if (Name == null)
            {
                return "<Main>";
            }

            return $"<Fn {Name}>";
        }
    }
}
