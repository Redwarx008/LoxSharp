using LoxSharp.Core.Utility;

namespace LoxSharp.Core
{
    internal class BoundMethod
    {
        public Value Receiver { get; private set; }
        public Function Function { get; private set; }

        public BoundMethod(Value receiver, Function function)
        {
            Receiver = receiver;
            Function = function;
        }

        public override string ToString()
        {
            return Function.ToString();
        }
    }
}
