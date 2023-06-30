using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal struct Value
    {
        internal enum ValueType
        {
            Null,
            Bool,
            Double
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Data
        {
            [FieldOffset(0)]
            public double asDouble;

            [FieldOffset(0)]
            public bool asBool;

            [FieldOffset (0)]
            public object asObject;
        }

        public ValueType Type { get; private set; }  

        public Data Val { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public bool IsNull() => Type == ValueType.Null;

        public static Value New(double val)
            => new Value() { Type = ValueType.Double, Val = new Data() { asDouble = val} };
        public static Value New(bool val)
            => new Value() { Type = ValueType.Bool, Val = new Data() { asBool = val } };
    }
}
