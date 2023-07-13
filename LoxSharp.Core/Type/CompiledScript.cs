namespace LoxSharp.Core
{
    internal class CompiledScript
    {
        public Function Main { get; private set; }
        public CompiledScript(Function main)
        {
            Main = main;
        }
    }
}
