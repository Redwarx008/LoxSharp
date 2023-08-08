using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpES.Core
{
    public readonly struct Value : IEquatable<Value>
    {
        private readonly ValueType _type;
        private readonly BasicData _data;
        private readonly object? _obj;

        public enum ValueType
        {
            Undefined,
            Null,
            Bool,
            Double,
            String,
            Function,
            ForeignFunction,
            ForeignMethod,
            Class,
            Module,
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
        internal readonly Class AsClass
        {
            get
            {
                Debug.Assert(IsClass);
                return (Class)_obj!;
            }
        }
        internal readonly Module AsModule
        {
            get
            {
                Debug.Assert(IsModule);
                return (Module)_obj!;
            }
        }
        internal readonly ClassInstance AsInstance
        {
            get
            {
                Debug.Assert(IsInstance);
                return (ClassInstance)_obj!;
            }
        }
        internal readonly BoundMethod AsBoundMethod
        {
            get
            {
                Debug.Assert(IsBoundMethod);
                return (BoundMethod)_obj!;
            }
        }
        internal readonly ForeignFunction AsForeignFunction
        {
            get
            {
                Debug.Assert(IsForeignFunction);
                return (ForeignFunction)_obj!;
            }
        }
        internal readonly ForeignMethod AsForeignMethod
        {
            get
            {
                Debug.Assert(IsForeignMethod);
                return (ForeignMethod)_obj!;
            }
        }
        internal readonly object? AsObject
        {
            get
            {
                return _obj;
            }
        }

        public readonly ValueType Type => _type;

        internal readonly bool IsUndefined => _type == ValueType.Undefined;
        public readonly bool IsNull => _type == ValueType.Null;
        public readonly bool IsBool => _type == ValueType.Bool;
        public readonly bool IsNumber => _type == ValueType.Double;
        public readonly bool IsString => _type == ValueType.String;
        public readonly bool IsFunction => _type == ValueType.Function;
        public readonly bool IsClass => _type == ValueType.Class;
        internal readonly bool IsModule => _type == ValueType.Module;
        public readonly bool IsInstance => _type == ValueType.Instance;
        public readonly bool IsBoundMethod => _type == ValueType.BoundMethod;
        public readonly bool IsForeignFunction => _type == ValueType.ForeignFunction;
        public readonly bool IsForeignMethod => _type == ValueType.ForeignMethod;
        public Value(double val)
        {
            _data = new BasicData()
            {
                Double = val,
            };
            _type = ValueType.Double;
            _obj = null;
        }

        public Value(bool val)
        {
            _data = new BasicData()
            {
                Bool = val,
            };
            _type = ValueType.Bool;
            _obj = null;
        }

        public Value(string val)
        {
            _data = default;
            _type = ValueType.String;
            _obj = val;
        }

        internal Value(Function val)
        {
            _data = default;
            _type = ValueType.Function;
            _obj = val;
        }

        public Value(Class val)
        {
            _data = default;
            _type = ValueType.Class;
            _obj = val;
        }

        internal Value(Module val)
        {
            _data = default;
            _type = ValueType.Module;
            _obj = val;
        }

        internal Value(ClassInstance val)
        {
            _data = default;
            _type = ValueType.Instance;
            _obj = val;
        }

        internal Value(BoundMethod val)
        {
            _data = default;
            _type = ValueType.BoundMethod;
            _obj = val;
        }

        public Value(ForeignFunction val)
        {
            _data = default;
            _type = ValueType.ForeignFunction;
            _obj = val;
        }

        public Value(ForeignMethod val)
        {
            _data = default;
            _type = ValueType.ForeignMethod;
            _obj = val;
        }
        /// <summary>
        /// Feature 'parameterless struct constructors' is not available in C# 8.0. 
        /// </summary>
        //public Value()
        //{
        //    _data = default;
        //    _type = ValueType.Null;
        //    _obj = null;
        //}

        private Value(BasicData data, ValueType type, object? obj)
        {
            _data = data;
            _type = type;
            _obj = obj;
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
                case ValueType.Module:
                    return AsModule == other.AsModule;
                case ValueType.Instance:
                    return AsInstance == other.AsInstance;
                case ValueType.BoundMethod:
                    return AsBoundMethod == other.AsBoundMethod;
                case ValueType.ForeignFunction:
                    return AsForeignFunction == other.AsForeignFunction;
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
                case ValueType.ForeignFunction:
                case ValueType.ForeignMethod:
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
                case ValueType.ForeignFunction:
                    return AsForeignFunction.ToString();
                case ValueType.ForeignMethod:
                    return AsForeignMethod.ToString();
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
            if (a.IsString && b.IsString)
            {
                return new Value(a.AsString + b.AsString);
            }
            if (a.IsNumber && b.IsString)
            {
                return new Value(a.ToString() + b.AsString);
            }
            if (a.IsString && b.IsNumber)
            {
                return new Value(a.AsString + b.ToString());
            }

            return Value.NUll;
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

        public static Value operator %(in Value a, in Value b)
        {
            if (a.IsNumber && b.IsNumber)
            {
                return new Value(a.AsDouble % b.AsDouble);
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

        internal static Value Undefined(string name)
        {
            return new Value(default, ValueType.Undefined, name);
        }

        public static Value NUll => new Value(default, ValueType.Null, null);

    }
}
