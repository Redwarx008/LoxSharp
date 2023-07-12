using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace LoxSharp.Core
{
    public struct Value : IEquatable<Value>
    {
        private ValueType _type;
        private BasicData _data;
        private object? _obj = null;

        internal enum ValueType
        {
            Undefined,
            Null,
            Bool,
            Double,
            String,
            Function,
            Class,
            Instance,
            BoundMethod,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct BasicData
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
                Debug.Assert(IsString || IsUndefined);
                return (string)_obj!;
            }
        }
        internal readonly Function AsFunction
        {
            get
            {
                Debug.Assert(IsFunction);
                return (Function)_obj!;
            }
        }
        public readonly InternalClass AsClass
        {
            get
            {
                Debug.Assert(IsClass);
                return (InternalClass)_obj!;
            }
        }
        public readonly ClassInstance AsInstance
        {
            get
            {
                Debug.Assert(IsInstance);
                return (ClassInstance)_obj!;
            }
        }
        public readonly BoundMethod AsBoundMethod
        {
            get
            {
                Debug.Assert(IsBoundMethod);
                return (BoundMethod)_obj!;
            }
        }


        public readonly ValueType Type => _type;

        public readonly bool IsUndefined => _type == ValueType.Undefined;
        public readonly bool IsNull => _type == ValueType.Null;
        public readonly bool IsBool => _type == ValueType.Bool;
        public readonly bool IsNumber => _type == ValueType.Double;
        public readonly bool IsString => _type == ValueType.String;
        public readonly bool IsFunction => _type == ValueType.Function;
        public readonly bool IsClass => _type == ValueType.Class;
        public readonly bool IsInstance => _type == ValueType.Instance;
        public readonly bool IsBoundMethod => _type == ValueType.BoundMethod;

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

        public Value(InternalClass val)
        {
            _data = default;
            _type = ValueType.Class;
            _obj = val;
        }

        public Value(ClassInstance val)
        {
            _data = default;
            _type = ValueType.Instance;
            _obj = val;
        }

        public Value(BoundMethod val)
        {
            _data = default;
            _type = ValueType.BoundMethod;
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
                case ValueType.Function:
                    return AsFunction == other.AsFunction;
                case ValueType.Class:
                    return AsClass == other.AsClass;
                case ValueType.Instance:
                    return AsInstance == other.AsInstance;
                case ValueType.BoundMethod:
                    return AsBoundMethod == other.AsBoundMethod;
                case ValueType.Null:
                    return true;
                default: return false; // Unreachable.
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
            {
                return false;
            }
            return obj is Value value && Equals(value);
        }

        public override int GetHashCode()
        {
            switch (_type)
            {
                case ValueType.Bool:
                    return AsBool.GetHashCode();
                case ValueType.Double:
                    return AsDouble.GetHashCode();
                case ValueType.Null:
                case ValueType.String:
                case ValueType.Function:
                case ValueType.Class:
                case ValueType.Instance:
                case ValueType.BoundMethod:
                default:
                    return _obj!.GetHashCode();
            }
        }

        public override string ToString()
        {
            switch (_type)
            {
                case ValueType.Undefined:
                    return $"undefined {AsString}";
                case ValueType.Null:
                    return "null";
                case ValueType.Bool:
                    return AsBool.ToString();
                case ValueType.Double:
                    return AsDouble.ToString();
                case ValueType.String:
                    return $"'{AsString}'";
                case ValueType.Function:
                    return AsFunction.ToString();
                case ValueType.Class:
                    return AsClass.ToString();
                case ValueType.Instance:
                    return AsInstance.ToString();
                case ValueType.BoundMethod:
                    return AsBoundMethod.ToString();
                default:
                    return "type not implemented";
            }
        }
        public static Value operator +(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble + b.AsDouble);
            }
            else if (a.IsString && b.IsString)
            {
                return new Value(a.AsString + b.AsString);
            }
            else if (a.IsString && b.IsNumber)
            {
                return new Value(a.AsString + b.AsDouble.ToString());
            }
            return new Value();
        }

        public static Value operator -(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble - b.AsDouble);
            }
            return new Value();
        }

        public static Value operator *(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble * b.AsDouble);
            }
            return new Value();
        }

        public static Value operator /(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble / b.AsDouble);
            }
            return new Value();
        }

        public static Value operator ==(in Value a, in Value b)
        {
            return new Value(a.Equals(b));
        }
        public static Value operator !=(in Value a, in Value b)
        {
            return !(a == b);
        }

        public static Value operator !(in Value val)
        {
            bool boolean = val.IsNull || (val.IsBool && val.AsBool == false);
            return new Value(boolean);
        }

        public static Value operator >(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble > b.AsDouble);
            }
            return new Value();
        }

        public static Value operator <(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble < b.AsDouble);
            }
            return new Value();
        }

        public static Value NewUndefined(string name)
        {
            return new Value
            {
                _type = ValueType.Undefined,
                _obj = name,
                _data = default
            };
        }

    }
}
