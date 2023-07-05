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

        public T this[int i]
        {
            get => _values[i];  
            set => _values[i] = value;  
        }

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

        public T Peek(int distance = 0)
        {
            return _values[_stackTopIndex - 1 - distance];
        }
    }
}
