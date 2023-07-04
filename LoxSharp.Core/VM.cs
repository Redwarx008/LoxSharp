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
    
    internal class VM
    {

        private Chunk _currentChunk = null!;
        private int _ip = 0;
        private StackList<Value> _stack = new();
        private Table _globals = new();
        public InterpretResult Interpret(Chunk chunk)
        {
            _currentChunk = chunk;
            return Run();
        }

        private InterpretResult Run()
        {
            while(true)
            {
                OpCode instruction = (OpCode)ReadByte();
                switch (instruction) 
                {
                    case OpCode.CONSTANT:
                        Value constant = ReadConstant();
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
                    case OpCode.Pop:
                        _stack.Pop();   
                        break;
                    case OpCode.Define_Global:
                        string variableName = ReadConstant().AsString;
                        _globals[variableName] = _stack.Pop(); 
                        break;
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
                    case OpCode.Print:
                        Console.WriteLine(_stack.Pop().ToString());
                        break;
                    case OpCode.RETURN:
                        return InterpretResult.OK;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private byte ReadByte()
        {
            return _currentChunk.Instructions[_ip++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        private Value ReadConstant()
        {
            return _currentChunk.Constants[ReadByte()];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowRuntimeError(string message)
        {
            int line = _currentChunk.LineNumbers[_ip];
            throw new RuntimeException($"[Line {line}] {message}");    
        }
    }
}
