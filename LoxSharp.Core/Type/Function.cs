namespace LoxSharp.Core
{
    internal class Function
    {
        public int Arity { get; set; }
        public Chunk Chunk { get; private set; }
        /// <summary>
        /// The module where this function was defined.
        /// </summary>
        public Module Module { get; set; }
        public string? Name { get; set; }

        public Function(Module module)
        {
            Chunk = new Chunk();
            Arity = 0;
            Module = module;
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
