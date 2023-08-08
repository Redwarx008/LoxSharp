using System;
using System.Runtime.CompilerServices;

namespace SharpES.Core
{
    internal class ValueStack<T> where T : struct
    {
        private T[] _values;

        private int _size = 0;

        public ValueStack(int capacity)
        {
            _values = new T[capacity];
        }

        public int Count => _size;

        public ref T this[int i]
        {
            get => ref _values[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T value)
        {
            _values[_size++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException("empty stack");
            }
            T item = _values[--_size];
            _values[_size] = default;   // Free memory quicker.
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Peek(int distance = 0)
        {
            return ref _values[_size - 1 - distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Discard(int count)
        {
            //_size -= count;
            for (int i = 0; i < count; ++i)
            {
                _values[--_size] = default;
            }
        }

        public ArraySegment<T> TopSlice(int count)
        {
            return new ArraySegment<T>(_values, _size - count, count);
        }

    }
}
