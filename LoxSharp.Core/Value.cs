using LoxSharp.Core.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal struct Value : IEquatable<Value>
    {
        private ValueType _type;
        private BasicData _data;
        private object? _obj = null;

        internal enum ValueType
        {
            Null,
            Bool,
            Double,
            String,
            Function,
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct BasicData
        {
            [FieldOffset(0)]
            public double Double;

            [FieldOffset(0)]
            public bool Bool;
        }
        public readonly double AsDouble
        {
            get
            {
                Debug.Assert(IsNumber);
                return _data.Double;
            }
        }
        public readonly bool AsBool
        {
            get
            {
                Debug.Assert(IsBool);
                return _data.Bool;
            }
        }
        public readonly string AsString
        {
            get
            {
                Debug.Assert(IsString);
                return (string)_obj!;
            }
        }
        public readonly Function AsFunction
        {
            get
            {
                Debug.Assert(IsFunction);
                return (Function)_obj!;
            }
        }

        public readonly bool IsNull => _type == ValueType.Null;
        public readonly bool IsBool => _type == ValueType.Bool;
        public readonly bool IsNumber => _type == ValueType.Double;
        public readonly bool IsString => _type == ValueType.String;  
        public readonly bool IsFunction => _type == ValueType.Function; 

        public Value(double val)
        {
            _data = new BasicData()
            {
                Double = val,
            };
            _type = ValueType.Double;
        }

        public Value(bool val)
        {
            _data = new BasicData()
            {
                Bool = val,
            };
            _type = ValueType.Bool;
        }

        public Value(string val)
        {
            _data = default;
            _type = ValueType.String;
            _obj = val; 
        }

        public Value(Function val)
        {
            _data = default;
            _type = ValueType.Function;
            _obj = val;
        }

        public Value()
        {
            _data = default;
            _type = ValueType.Null;
            _obj = null;    
        }

        public bool Equals(Value other)
        {
            if (_type != other._type)
            {
                return false;
            }
            switch (_type)
            {
                case ValueType.Bool:
                    return AsBool == other.AsBool;
                case ValueType.Double:
                    return AsDouble == other.AsDouble;
                case ValueType.String:
                    return AsString == other.AsString;
                case ValueType.Null:
                    return true;
                default: return false; // Unreachable.
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if(obj == null)
            {
                return false;
            }
            return obj is Value value && Equals(value);  
        }

        public override int GetHashCode()
        {
            switch(_type) 
            {
                case ValueType.Bool:
                    return AsBool.GetHashCode();
                case ValueType.Double:
                    return AsDouble.GetHashCode();
                case ValueType.Null:
                case ValueType.String:
                default:
                    return _obj!.GetHashCode();
            }
        }

        public override string ToString()
        {
            switch(_type)
            {
                case ValueType.Null:
                    return "null";
                case ValueType.Bool:
                    return AsBool.ToString();
                case ValueType.Double:
                    return AsDouble.ToString();
                case ValueType.String:
                    return AsString;
                case ValueType.Function:
                    return AsFunction.ToString();   
                default:
                    return "type not implemented";
            }
        }
        public static Value operator +(Value a, Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble + b.AsDouble);
            }
            else if(a.IsString && b.IsString)
            {
                return new Value(a.AsString + b.AsString);  
            }
            else if(a.IsString && b.IsNumber)
            {
                return new Value(a.AsString + b.AsDouble.ToString());
            }
            return new Value(); 
        }

        public static Value operator -(Value a, Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble - b.AsDouble);
            }
            return new Value();
        }

        public static Value operator *(Value a, Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble * b.AsDouble);
            }
            return new Value();
        }

        public static Value operator /(Value a, Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble / b.AsDouble);
            }
            return new Value();
        }

        public static Value operator ==(Value a, Value b)
        {
            return new Value(a.Equals(b));
        }
        public static Value operator !=(Value a, Value b)
        {
            return !(a == b);
        }

        public static Value operator !(Value val)
        {
            bool boolean = val.IsNull || (val.IsBool && val.AsBool == false);
            return new Value(boolean);
        }

        public static Value operator >(Value a, Value b)
        {
            if(a.IsNumber && b.IsNumber)
            {
                return  new Value(a.AsDouble > b.AsDouble); 
            }
            return new Value();
        }

        public static Value operator <(Value a, Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble < b.AsDouble);
            }
            return new Value();
        }

    }
}
