using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core.Utility
{
    internal class StackList<T>
    {
        private static int STACK_MAX = 256;

        private T[] _values = new T[STACK_MAX];

        private int _stackTopIndex = 0; 

        public void Push(T value)
        {
            _values[_stackTopIndex] = value;
            ++_stackTopIndex;
        }

        public T Pop() 
        {
            --_stackTopIndex;
            return _values[_stackTopIndex];
        }

        public T Peek(int distance)
        {
            return _values[_stackTopIndex - 1 - distance];
        }
    }
}
