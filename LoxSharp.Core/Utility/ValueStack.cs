using System.Runtime.CompilerServices;

namespace LoxSharp.Core
{
    internal class ValueStack<T> where T : struct
    {
        private T[] _values;

        private int _stackTopIndex = 0;

        public ValueStack(int capacity)
        {
            _values = new T[capacity];
        }

        public int Count => _stackTopIndex;

        public ref T this[int i]
        {
            get => ref _values[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T value)
        {
            _values[_stackTopIndex] = value;
            ++_stackTopIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            --_stackTopIndex;
            return _values[_stackTopIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Peek(int distance = 0)
        {
            return ref _values[_stackTopIndex - 1 - distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Discard(int count)
        {
            _stackTopIndex -= count;
        }

    }
}
