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
        private object? _obj = null;
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

        public readonly double AsDouble => _data.Double;
        public readonly bool AsBool => _data.Bool;

        public bool IsNull => Type == ValueType.Null;

        public bool IsFalsey => Type == ValueType.Null || (Type == ValueType.Bool && AsBool == false);

        public Value(double val)
        {
            _data = new BasicData()
            {
                Double = val,
            };
            Type = ValueType.Double;
        }

        public Value(bool val)
        {
            _data = new BasicData()
            {
                Bool = val,
            };
            Type = ValueType.Bool;
        }

        public Value()
        {
            _data = default;
            Type = ValueType.Null;
        }

        public static Value operator +(Value a, Value b)
        {
            if (a.Type == ValueType.Double && b.Type == ValueType.Double)
            {
                return new Value(a.AsDouble + b.AsDouble);
            }
            throw new RuntimeException("Operands must be two numbers or two strings.");
        }

        public static Value operator -(Value a, Value b)
        {
            if (a.Type == ValueType.Double && b.Type == ValueType.Double)
            {
                return new Value(a.AsDouble - b.AsDouble);
            }
            throw new RuntimeException("Operands must be two numbers.");
        }

        public static Value operator *(Value a, Value b)
        {
            if (a.Type == ValueType.Double && b.Type == ValueType.Double)
            {
                return new Value(a.AsDouble * b.AsDouble);
            }
            throw new RuntimeException("Operands must be two numbers.");
        }

        public static Value operator /(Value a, Value b)
        {
            if (a.Type == ValueType.Double && b.Type == ValueType.Double)
            {
                return new Value(a.AsDouble / b.AsDouble);
            }
            throw new RuntimeException("Operands must be two numbers.");
        }
    }
}
