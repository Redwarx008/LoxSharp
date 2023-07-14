namespace LoxSharp.Core
{
    internal class BoundMethod
    {
        public Value Receiver { get; private set; }
        public Value Function { get; private set; }

        public BoundMethod(Value receiver, Value function)
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
