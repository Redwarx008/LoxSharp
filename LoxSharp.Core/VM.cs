using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LoxSharp.Core.Utility;

namespace LoxSharp.Core
{
    using Table = Dictionary<string, Value>;
    internal enum InterpretResult
    {
        OK,
        COMPILE_ERROR,
        RUNTIME_ERROR,
    }
    
    internal struct CallFrame
    {
        public Function Function { get; private set; }
        public int Ip { get; set; } = 0;
        public int StackStart { get; private set; }

        public CallFrame(Function function, int stackStart)
        {
            Function = function;
            StackStart = stackStart;
        }
    }

    internal class VM
    {
        private const int STACK_MAX = 256;
        private const int FRAME_MAX = 64;

        private ValueStack<CallFrame> _callFrames = new(FRAME_MAX);
        private ValueStack<Value> _stack = new(FRAME_MAX * STACK_MAX);
        private Table _globals = new();

        public InterpretResult Interpret(Function function)
        {
            _stack.Push(new Value(function));

            CallFrame callframe = new(function, 0);
            _callFrames.Push(callframe);

            return Run();
        }

        private InterpretResult Run()
        {  
            while(true)
            {
                ref CallFrame frame = ref _callFrames.Peek();
#if DEBUG
                Disassembler disassembler = Disassembler.Instance;

                if (disassembler != null)
                {
                    disassembler.DisassembleStack(_stack);
                    disassembler.DisassembleInstruction(frame.Function.Chunk, frame.Ip);
                    Console.Write(disassembler.GetText());
                }
#endif
                OpCode instruction = (OpCode)ReadByte(ref frame);

                switch (instruction) 
                {
                    case OpCode.CONSTANT:
                        Value constant = ReadConstant(ref frame);
                        _stack.Push(constant);  
                        break;
                    case OpCode.NIL:
                        _stack.Push(new Value());
                        break;
                    case OpCode.TRUE:
                        _stack.Push(new Value(true));
                        break;
                    case OpCode.FALSE:  
                        _stack.Push(new Value(false));  
                        break;
                    case OpCode.POP:
                        _stack.Pop();   
                        break;
                    case OpCode.GET_LOCAL:
                        {
                            int slot = ReadByte(ref frame) + frame.StackStart;
                            _stack.Push(_stack[slot]);
                            break;
                        }
                    case OpCode.SET_LOCAL:
                        {
                            int slot = ReadByte(ref frame) + frame.StackStart;
                            _stack[slot] = _stack.Peek();
                            break;
                        }
                    case OpCode.DEFINE_GLOBAL:
                        {
                            string variableName = ReadConstant(ref frame).AsString;
                            _globals[variableName] = _stack.Pop();
                            break;
                        }
                    case OpCode.GET_GLOBAL:
                        {
                            string variableName = ReadConstant(ref frame).AsString;
                            if(!_globals.TryGetValue(variableName, out var value))
                            {
                                ThrowRuntimeError($"Undefined variable {variableName}");
                            }
                            else
                            {
                                _stack.Push(value);
                            }
                            break;
                        }
                    case OpCode.SET_GLOBAL:
                        {
                            string variableName = ReadConstant(ref frame).AsString;
                            if(!_globals.ContainsKey(variableName))
                            {
                                ThrowRuntimeError($"Undefined variable {variableName}");
                            }
                            else
                            {
                                _globals[variableName] = _stack.Peek(0);
                            }
                            break;
                        }
                    case OpCode.GET_PROPERTY:
                        {
                            if (!_stack.Peek().IsInstance)
                            {
                                ThrowRuntimeError("Only instances have properties.");
                            }

                            Instance instance = _stack.Peek().AsInstance;
                            string propertyName = ReadConstant(ref frame).AsString; 
                            if (instance.Fields.TryGetValue(propertyName, out var value))
                            {
                                _stack[_stack.Count - 1] = value;
                                break;
                            }

                            // if property is a method
                            BindMethod(instance.Class, propertyName);
                            break;
                        }
                    case OpCode.SET_PROPERTY:
                        {
                            if (!_stack.Peek(1).IsInstance)
                            {
                                ThrowRuntimeError("Only instances have fields.");
                            }

                            Instance instance = _stack.Peek(1).AsInstance;
                            string fieldName = ReadConstant(ref frame).AsString;
                            instance.Fields[fieldName] = _stack.Peek();

                            // remove the second element from the stack.
                            Value val = _stack.Pop();
                            _stack.Pop();
                            _stack.Push(val);
                            break;
                        }
                    case OpCode.EQUAL:
                    case OpCode.GREATER:
                    case OpCode.LESS:
                    case OpCode.ADD:
                    case OpCode.SUBTRACT:
                    case OpCode.MULTIPLY:
                    case OpCode.DIVIDE:
                        BinaryOperator(instruction);
                        break;
                    case OpCode.NOT:
                        _stack.Push(!_stack.Pop());  
                        break;
                    case OpCode.NEGATE:
                        if(!_stack.Peek(0).IsNumber)
                        {
                            ThrowRuntimeError("Operand must be a number.");
                        }
                        _stack.Push(new Value(-_stack.Pop().AsDouble));
                        break;
                    case OpCode.PRINT:
                        Console.WriteLine(_stack.Pop().ToString());
                        break;
                    case OpCode.JUMP:
                        {
                            ushort offset = ReadUShort(ref frame);
                            frame.Ip += offset;
                            break;
                        }
                    case OpCode.JUMP_IF_FALSE:
                        {
                            ushort offset = ReadUShort(ref frame);
                            if(!_stack.Peek().AsBool)
                            {
                                frame.Ip += offset;
                            }
                            break;
                        }
                    case OpCode.LOOP:
                        {
                            ushort offset = ReadUShort(ref frame);
                            frame.Ip -= offset;  
                            break;
                        }
                    case OpCode.CALL:
                        {
                            int argCount = ReadByte(ref frame);
                            CallValue(in _stack.Peek(argCount), argCount);
                            break;
                        }
                    case OpCode.RETURN:
                        {
                            Value result = _stack.Pop();
                            _callFrames.Pop();
                            if(_callFrames.Count == 0)
                            {
                                _ = _stack.Pop();
                                return InterpretResult.OK;
                            }
                            _stack.Discard(_stack.Count - frame.StackStart);
                            _stack.Push(result);    
                            break;
                        }
                    case OpCode.CLASS:
                        {
                            string className = ReadConstant(ref frame).AsString;
                            _stack.Push(new Value(new InternalClass(className)));
                            break;
                        }
                    case OpCode.CLASS_METHOD:
                        {
                            string methodName = ReadConstant(ref frame).AsString;   
                            Value method = _stack.Peek();
                            InternalClass internalClass = _stack.Peek(1).AsClass;
                            internalClass.Methods[methodName] = method.AsFunction;
                            _stack.Pop();
                            break;
                        }
                }
            }
        }

        #region Assistant method

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private byte ReadByte(ref CallFrame callFrame)
        {
            return callFrame.Function.Chunk.Instructions[callFrame.Ip++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private ushort ReadUShort(ref CallFrame callFrame) 
        {
            var high = callFrame.Function.Chunk.Instructions[callFrame.Ip++];    
            var low = callFrame.Function.Chunk.Instructions[callFrame.Ip++];    
            return (ushort)(high << 8 | low);   
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private Value ReadConstant(ref CallFrame callFrame)
        {
            return callFrame.Function.Chunk.Constants[ReadByte(ref callFrame)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BinaryOperator(OpCode op)
        {
            Value b = _stack.Pop();
            Value a = _stack.Pop();

            Value res;
            switch (op)
            {
                case OpCode.EQUAL:
                    _stack.Push(a == b);
                    break;
                case OpCode.GREATER:
                    res = a > b;
                    if (res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.LESS:
                    res = a < b;
                    if (res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.ADD:
                    res = a + b;  
                    if(res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers or left operand is string.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.SUBTRACT:
                    res = a - b;  
                    if(res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.MULTIPLY:
                    res = a * b;
                    if(res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.DIVIDE:
                    res = a / b;
                    if(res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
            }
        }

        private void CallValue(in Value callee, int argCount)
        {
            switch(callee.Type)
            {
                case Value.ValueType.Function:
                    Call(callee.AsFunction, argCount);
                    break;
                case Value.ValueType.BoundMethod:
                    BoundMethod boundMethod = callee.AsBoundMethod;
                    _stack[_stack.Count - argCount - 1] = boundMethod.Receiver;
                    Call(boundMethod.Function, argCount);
                    break;
                case Value.ValueType.Class:
                    CreateInstance(callee.AsClass, argCount);
                    break;
                default:
                    ThrowRuntimeError("Can only call functions and classes.");
                    break;// Non-callable object type.
            }
        }

        private void Call(Function function, int argCount)
        {
            if (argCount != function.Arity)
            {
                ThrowRuntimeError($"Expected {function.Arity} arguments but got {argCount}.");
            }

            if (_callFrames.Count == FRAME_MAX)
            {
                ThrowRuntimeError("Stack overflow.");
            }

            CallFrame callFrame = new(function, _stack.Count - argCount - 1);
            _callFrames.Push(callFrame);    
        }

        private void CreateInstance(InternalClass internalClass, int argCount)
        {
            Instance instance = new(internalClass);
            _stack[_stack.Count - 1 - argCount] = new Value(instance);  
        }

        private void BindMethod(InternalClass internalClass, string methodName)
        {
            if (!internalClass.Methods.TryGetValue(methodName, out var method)) 
            {
                ThrowRuntimeError($"Undefined property '{methodName}'");
            }
            else
            {
                BoundMethod boundMethod = new(_stack.Peek(), method);
                _stack[_stack.Count - 1] = new Value(boundMethod);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowRuntimeError(string message)
        {
            string errorMsg = $"{message}\n";
            // print method call stack.
            for(int i = _callFrames.Count - 1; i >= 0; --i)
            {
                ref readonly CallFrame frame = ref _callFrames[i];
                Function function = frame.Function;
                int line = function.Chunk.LineNumbers[frame.Ip - 1];
                errorMsg += $"[Line {line}] in ";
                if(function.Name == null)
                {
                    errorMsg += "Main\n";
                }
                else
                {
                    errorMsg += $"{function.Name}\n";
                }
            }
            throw new RuntimeException(errorMsg);    
        }
        #endregion
    }
}
