using System.Runtime.CompilerServices;

namespace LoxSharp.Core
{
    internal struct CallFrame
    {
        public InternalClass? Class { get; set; } = null;
        public Function Function { get; private set; }
        public int Ip { get; set; } = 0;
        public int StackStart { get; private set; }

        public CallFrame(Function function, int stackStart)
        {
            Function = function;
            StackStart = stackStart;
        }
    }

    public class VM
    {
        private const int STACK_MAX = 256;
        private const int FRAME_MAX = 64;

        private ValueStack<CallFrame> _callFrames = new(FRAME_MAX);
        private ValueStack<Value> _stack = new(FRAME_MAX * STACK_MAX);
        private List<Value> _globalValues = null!;

        private List<byte> _currentInstructions = null!;
        private List<Value> _currentConstants = null!;

        public VM(List<Value> globalValues)
        {
            _globalValues = globalValues;
        }

        internal void Interpret(CompiledScript compiledScript)
        {
            _stack.Push(new Value(compiledScript.Main));

            CallFrame callframe = new(compiledScript.Main, 0);
            _currentInstructions = callframe.Function.Chunk.Instructions;
            _currentConstants = callframe.Function.Chunk.Constants;
            _callFrames.Push(callframe);
            Run();
        }

        private void Run()
        {
            while (true)
            {
                ref CallFrame frame = ref _callFrames.Peek();
#if DEBUG
                Disassembler disassembler = Disassembler.Instance;

                if (disassembler != null)
                {
                    disassembler.DisassembleStack(_stack);
                    disassembler.DisassembleInstruction(frame.Function.Chunk, frame.Ip, _globalValues);
                    Console.Write(disassembler.GetText());
                }
#endif
                OpCode instruction = (OpCode)ReadByte(ref frame);

                switch (instruction)
                {
                    case OpCode.CONSTANT_8:
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
                            byte index = ReadByte(ref frame);
                            _globalValues[index] = _stack.Pop();
                            break;
                        }
                    case OpCode.GET_GLOBAL:
                        {
                            byte index = ReadByte(ref frame);
                            Value val = _globalValues[index];
                            if (val.IsUndefined)
                            {
                                ThrowRuntimeError($"Undefined variable '{val.AsString}'");
                            }
                            else
                            {
                                _stack.Push(val);
                            }
                            break;
                        }
                    case OpCode.SET_GLOBAL:
                        {
                            byte index = ReadByte(ref frame);
                            Value val = _globalValues[index];
                            if (val.IsUndefined)
                            {
                                ThrowRuntimeError($"Undefined variable '{val.AsString}'");
                            }
                            else
                            {
                                _globalValues[index] = _stack.Peek(0);
                            }
                            break;
                        }
                    case OpCode.GET_PROPERTY:
                        {
                            if (!_stack.Peek().IsInstance)
                            {
                                ThrowRuntimeError("Only instances have properties.");
                            }

                            ClassInstance instance = _stack.Peek().AsInstance;
                            string propertyName = ReadConstant(ref frame).AsString;
                            if (instance.Fields.TryGetValue(propertyName, out var value))
                            {
                                _stack[_stack.Count - 1] = value;
                                break;
                            }

                            // if property is a method
                            CreateBindMethod(instance.Class, propertyName);
                            break;
                        }
                    case OpCode.SET_PROPERTY:
                        {
                            if (!_stack.Peek(1).IsInstance)
                            {
                                ThrowRuntimeError("Only instances have fields.");
                            }

                            ClassInstance instance = _stack.Peek(1).AsInstance;
                            string fieldName = ReadConstant(ref frame).AsString;

                            if (instance.Fields.ContainsKey(fieldName) || frame.Function.Name == "init")
                            {
                                instance.Fields[fieldName] = _stack.Peek();

                                // remove the second element from the stack.
                                Value val = _stack.Pop();
                                _stack.Pop();
                                _stack.Push(val);
                            }
                            else
                            {
                                ThrowRuntimeError($"undefined property {fieldName}");
                            }
                            break;
                        }
                    case OpCode.GET_INDEX:
                        {
                            switch(_stack.Peek(1).AsObject)
                            {
                                case ArrayInstance arrayInstance:
                                    {
                                        if (!_stack.Peek().IsNumber)
                                        {
                                            ThrowRuntimeError("Expects a number to use as an index.");
                                        }
                                        double index = _stack.Peek().AsDouble;
                                        _stack.Discard(2);
                                        _stack.Push(arrayInstance.Values[(int)index]);
                                        break;
                                    }
                                // todo map
                                default:
                                    ThrowRuntimeError("Only arrays and maps can use index.");
                                    break;
                            }
                            break;  
                        }
                    case OpCode.SET_INDEX:
                        {
                            switch (_stack.Peek(2).AsObject)
                            {
                                case ArrayInstance arrayInstance:
                                    {
                                        if (!_stack.Peek(1).IsNumber)
                                        {
                                            ThrowRuntimeError("Expects a number to use as an index.");
                                        }
                                        double index = _stack.Peek(1).AsDouble;

                                        if (index >= arrayInstance.Values.Count)
                                        {
                                            ThrowRuntimeError("Array index out of bounds.");
                                        }

                                        ref Value val = ref _stack.Peek();
                                        _stack.Discard(3);
                                        arrayInstance.Values[(int)index] = val; 
                                        _stack.Push(val);
                                        break;
                                    }
                                    // todo map
                                    default :
                                    ThrowRuntimeError("Only arrays and maps can use index.");
                                    break;
                            }
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
                        if (!_stack.Peek().IsNumber)
                        {
                            ThrowRuntimeError("Operand must be a number.");
                        }
                        // _stack.Push(new Value(-_stack.Pop().AsDouble));
                        _stack.Peek() = new Value(-_stack.Peek().AsDouble);
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
                            if (!_stack.Peek().AsBool)
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
                    case OpCode.INVOKE:
                        {
                            string methodName = ReadConstant(ref frame).AsString;
                            int argCount = ReadByte(ref frame);
                            Invoke(methodName, argCount);
                            break;
                        }
                    case OpCode.RETURN:
                        {
                            Value result = _stack.Pop();
                            _callFrames.Pop();
                            
                            if (_callFrames.Count == 0)
                            {
                                _ = _stack.Pop();
                                return;
                            }

                            _currentInstructions = _callFrames.Peek().Function.Chunk.Instructions;
                            _currentConstants = _callFrames.Peek().Function.Chunk.Constants;

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
                            ref readonly Value method = ref _stack.Peek();
                            InternalClass internalClass = _stack.Peek(1).AsClass;
                            internalClass.Methods[methodName] = method;
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
            return _currentInstructions[callFrame.Ip++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort ReadUShort(ref CallFrame callFrame)
        {
            var high = _currentInstructions[callFrame.Ip++];
            var low = _currentInstructions[callFrame.Ip++];
            return (ushort)(high << 8 | low);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Value ReadConstant(ref CallFrame callFrame)
        {
            return _currentConstants[ReadByte(ref callFrame)];
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
                    if (res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers or left operand is string.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.SUBTRACT:
                    res = a - b;
                    if (res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.MULTIPLY:
                    res = a * b;
                    if (res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
                case OpCode.DIVIDE:
                    res = a / b;
                    if (res.IsNull)
                    {
                        ThrowRuntimeError("Operands must be numbers.");
                    }
                    _stack.Push(res);
                    break;
            }
        }

        private void CallValue(in Value callee, int argCount, InternalClass? scriptClass = null)
        {
            switch (callee.Type)
            {
                case Value.ValueType.InternalFunction:
                    CallInternalFunction(callee.AsFunction, argCount, scriptClass);
                    break;
                case Value.ValueType.BoundMethod:
                    CallBoundMethod(callee.AsBoundMethod, argCount);
                    break;
                case Value.ValueType.Class:
                    CreateInstance(callee.AsClass, argCount);
                    break;
                case Value.ValueType.HostFunction:
                    CallHostFunction(callee.AsHostFunction, argCount);  
                    break;
                default:
                    ThrowRuntimeError("Can only call functions and classes.");
                    break;// Non-callable object type.
            }
        }

        private void Invoke(string methodName, int argCount)
        {
            ref Value receiver = ref _stack.Peek(argCount);
            if (!receiver.IsInstance)
            {
                ThrowRuntimeError("Only instances have methods.");
            }
            ClassInstance instance = receiver.AsInstance;
            if (instance.Fields.TryGetValue(methodName, out var field))
            {
                _stack.Peek(argCount) = field;
                CallValue(field, argCount, instance.Class);
            }
            else
            {
                if (!instance.Class.Methods.TryGetValue(methodName, out var method))
                {
                    ThrowRuntimeError($"Undefined method {methodName}");
                }
                else
                {
                    switch (method.Type)
                    {
                        case Value.ValueType.InternalFunction:
                            CallInternalFunction(method.AsFunction, argCount, instance.Class);
                            break;
                        case Value.ValueType.HostMethod:
                            CallHostMethod(instance, method.AsHostMethod, argCount);
                            break;
                    }
                }
            }
        }

        private void InvokeFromClass(InternalClass internalClass, string methodName, int argCount)
        {
            if (!internalClass.Methods.TryGetValue(methodName, out var method))
            {
                ThrowRuntimeError($"Undefined method {methodName}");
            }
            else
            {
                switch (method.Type) 
                {
                    case Value.ValueType.InternalFunction:
                        CallInternalFunction(method.AsFunction, argCount, internalClass);
                        break;
                    case Value.ValueType.HostMethod:
                        CallHostFunction(method.AsHostFunction, argCount);
                        break;
                }
            }
        }

        private void CallInternalFunction(in Function function, int argCount, in InternalClass? scriptClass = null)
        {
            if (argCount != function.Arity)
            {
                ThrowRuntimeError($"Expected {function.Arity} arguments but got {argCount}.");
            }

            if (_callFrames.Count == FRAME_MAX)
            {
                ThrowRuntimeError("Stack overflow.");
            }

            CallFrame callFrame = new(function, _stack.Count - argCount - 1) { Class = scriptClass };
            _currentInstructions = callFrame.Function.Chunk.Instructions;
            _currentConstants = callFrame.Function.Chunk.Constants;
            _callFrames.Push(callFrame);
        }
        private void CallBoundMethod(BoundMethod boundMethod, int argCount)
        {
            _stack.Peek(argCount) = boundMethod.Receiver;

            switch (boundMethod.Function.Type)
            {
                case Value.ValueType.InternalFunction:
                    CallInternalFunction(boundMethod.Function.AsFunction, argCount);
                    break;
                case Value.ValueType.HostMethod:
                    CallHostMethod(boundMethod.Receiver.AsInstance, boundMethod.Function.AsHostMethod, argCount);
                    break;
            }
        }

        private void CreateInstance(InternalClass internalClass, int argCount)
        {

            ClassInstance instance = internalClass.CreateInstance();
 
            _stack.Peek(argCount) = new Value(instance);

            // call initializer
            if (internalClass.Methods.TryGetValue("init", out var method))
            {
                switch (method.Type)
                {
                    case Value.ValueType.InternalFunction:
                        CallInternalFunction(method.AsFunction, argCount, internalClass);
                        break;
                    case Value.ValueType.HostMethod:
                        CallHostMethod(instance, method.AsHostMethod, argCount);
                        break;
                }
            }
            else if (argCount != 0)
            {
                ThrowRuntimeError($"Expected 0 arguments but got {argCount}");
            }
        }

        private void CreateBindMethod(InternalClass internalClass, string methodName)
        {
            if (!internalClass.Methods.TryGetValue(methodName, out var method))
            {
                ThrowRuntimeError($"Undefined method '{methodName}'");
            }
            else
            {
                BoundMethod boundMethod = new(_stack.Peek(), method);
                _stack[_stack.Count - 1] = new Value(boundMethod);
            }
        }

        private void CallHostFunction(HostFunction function, int argCount)
        {
            Value[] args = new Value[argCount];
            for (int i = 0; i < argCount; ++i)
            {
                args[i] = _stack.Peek(argCount - (i + 1));
            }

            Value result = function.Function.Invoke(args);

            _stack.Discard(argCount + 1);
            _stack.Push(result);
        }

        private void CallHostMethod(ClassInstance instance, HostMethod method, int argCount)
        {
            Value[] args = new Value[argCount];
            for (int i = 0; i < argCount; ++i)
            {
                args[i] = _stack.Peek(argCount - (i + 1));
            }

            Value result = method.Method.Invoke(instance, args);

            _stack.Discard(argCount + 1);
            _stack.Push(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowRuntimeError(string message)
        {
            string errorMsg = $"Runtime error : {message}\n";
            // print method call stack.
            for (int i = _callFrames.Count - 1; i >= 0; --i)
            {
                ref readonly CallFrame frame = ref _callFrames[i];
                Function function = frame.Function;
                int line = function.Chunk.GetLineNumber(frame.Ip - 1);
                errorMsg += $"[Line {line}] in ";
                if (function.Name == null)
                {
                    errorMsg += "Main\n";
                }
                else
                {
                    if (frame.Class != null)
                    {
                        errorMsg += $"{frame.Class.Name}.{function.Name}\n";
                    }
                    else
                    {
                        errorMsg += $"{function.Name}\n";
                    }  
                }
            }
            throw new RuntimeException(errorMsg);
        }
        #endregion

        #region External call 
        public void CallFunction(in Value callee, params Value[] args)
        {
            _stack.Push(callee);    

            for(int i = 0; i < args.Length; ++i)
            {
                _stack.Push(args[i]);   
            }

            CallValue(callee, args.Length);
            Run();
        }
        #endregion
    }
}
