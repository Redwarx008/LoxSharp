using LoxSharp.Core.Utility;

namespace LoxSharp.Core
{
    internal class CompiledScript
    {
        public Function Main { get; private set; }
        public List<Value> GlobalValues { get; private set; }
        public CompiledScript(Function main,
            List<Value> globalValues)
        {
            Main = main;
            GlobalValues = globalValues;
        }
    }
}
