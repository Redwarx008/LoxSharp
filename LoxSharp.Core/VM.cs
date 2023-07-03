using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LoxSharp.Core.Utility;

namespace LoxSharp.Core
{
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
                    case OpCode.ADD:
                    case OpCode.SUBTRACT:
                    case OpCode.MULTIPLY:
                    case OpCode.DIVIDE:
                        BinaryOperator(instruction);
                        break;
                    case OpCode.NEGATE:
                        _stack.Push(Value.New(-_stack.Pop().AsDouble));
                        break;
                    case OpCode.RETURN:
                        Console.WriteLine(_stack.Pop());
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

            Value res = Value.New(0);
            switch(op)
            {
                case OpCode.ADD:
                    res.AsDouble = a.AsDouble + b.AsDouble;
                    break;
                case OpCode.SUBTRACT:
                    res.AsDouble = a.AsDouble - b.AsDouble; 
                    break;
                case OpCode.MULTIPLY:
                    res.AsDouble = a.AsDouble * b.AsDouble; 
                    break;
                case OpCode.DIVIDE:
                    res.AsDouble = a.AsDouble / b.AsDouble; 
                    break;
            }
            _stack.Push(res);
        }
    }
}
