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
        private BasicData _data;
        private object? _obj;
        internal enum ValueType
        {
            Null,
            Bool,
            Double
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct BasicData
        {
            [FieldOffset(0)]
            public double Double;

            [FieldOffset(0)]
            public bool Bool;
        }

        public ValueType Type { get; private set; }

        public double AsDouble
        {
            get => _data.Double;
            set => _data.Double = value;
        }
        public bool AsBool
        {
            get => _data.Bool;
            set => _data.Bool = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public bool IsNull() => Type == ValueType.Null;

        public static Value New(double val)
        {
            Value res = new Value();
        }
        public static Value New(bool val)
            => new Value() { Type = ValueType.Bool, _data = new BasicData() { Bool = val } };
        public static Value New()
            => new Value() { Type = ValueType.Null}
    }
}
