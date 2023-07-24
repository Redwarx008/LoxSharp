namespace LoxSharp.Core
{
    /// <summary>
    ///  Used as module function and class methods defined in script.
    /// </summary>
    public class Function
    {
        public int Arity { get; set; }
        internal Chunk Chunk { get; private set; }
        /// <summary>
        /// The module where this function was defined.
        /// </summary>
        internal Module Module { get; set; }
        public string? Name { get; set; }

        internal Function(Module module)
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
