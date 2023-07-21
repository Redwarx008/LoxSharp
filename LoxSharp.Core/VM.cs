using System.Diagnostics;
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

    internal class Coroutine
    {
        public ValueStack<CallFrame> CallFrames { get; private set; }
        public ValueStack<Value> Stack { get; private set; }
        /// <summary>
        /// The fiber that ran this one. If this fiber is yielded, control will resume to this one.
        /// </summary>
        public Coroutine? Caller { get; set; }

        public Coroutine(Function function)
        {
            CallFrames = new ValueStack<CallFrame>(VM.FRAME_MAX);
            Stack = new ValueStack<Value>(VM.STACK_MAX);

            CallFrames.Push(new CallFrame(function, Stack.Count));
            // The first slot always holds the function.
            Stack.Push(new Value(function));
        }
    }

    public class VM
    {
        internal const int STACK_MAX = 256;
        internal const int FRAME_MAX = 64;

        private ValueStack<CallFrame> _callFrames = new(FRAME_MAX);
        private ValueStack<Value> _stack = new(FRAME_MAX * STACK_MAX);

        private List<byte> _currentInstructions = null!;
        private List<Value> _currentConstants = null!;

        internal Module? LastLoadedModule { get; private set; }
        internal Dictionary<string, Module> LoadedModules { get; private set; }

        public ScriptConfiguration Config { get; private set; } 

        public VM(ScriptConfiguration config)
        {
            LoadedModules = new Dictionary<string, Module>();
            Config = config;
            InitCoreModule();
        }

        public VM() 
        {
            LoadedModules = new Dictionary<string, Module>();
            Config = new ScriptConfiguration();
            InitCoreModule();
        }

        internal InterpretResult Interpret(Function compiledScript)
        {
            _stack.Push(new Value(compiledScript));

            CallFrame callframe = new(compiledScript, 0);
            _currentInstructions = callframe.Function.Chunk.Instructions;
            _currentConstants = callframe.Function.Chunk.Constants;
            _callFrames.Push(callframe);
            return Run();
        }

        private void InitCoreModule()
        {
            Module coreModule = new Module(string.Empty);
            LoadedModules[coreModule.Name] = coreModule;
            coreModule.AddVariable(this, nameof(Array), new Value(new Array()));

            HostFunction printFunc = new("Print", (args) =>
            {
                if (Config.WriteFunction == null) return Value.NUll;
                Config.WriteFunction.Invoke(args[0].ToString());
                return Value.NUll;
            });
            coreModule.AddVariable(this, printFunc.Name, new Value(printFunc));

            //HostFunction GetClock = new("GetClock", (args) =>
            //{
            //    return new Value(DateTime.Now.TimeOfDay.TotalMilliseconds);
            //});
            //coreModule.AddVariable(this, GetClock.Name, new Value(GetClock));
        }

        private InterpretResult Run()
        {
            while (true)
            {
                ref CallFrame frame = ref _callFrames.Peek();
#if DEBUG
                Disassembler.DisassembleStack(_stack);
                Disassembler.DisassembleInstruction(frame.Function.Chunk, frame.Ip, frame.Function.Module.Variables);
#endif
                OpCode instruction = (OpCode)ReadByte(ref frame);

                switch (instruction)
                {
                    case OpCode.CONSTANT_8:
                        {
                            Value constant = ReadConstant8(ref frame);
                            _stack.Push(constant);
                            break;
                        }
                    case OpCode.CONSTANT_16:
                        {
                            Value constant = ReadConstant16(ref frame);
                            _stack.Push(constant);
                            break;
                        }
                    case OpCode.NULL:
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
                            int slot = ReadUShort(ref frame) + frame.StackStart;
                            _stack.Push(_stack[slot]);
                            break;
                        }
                    case OpCode.SET_LOCAL:
                        {
                            int slot = ReadUShort(ref frame) + frame.StackStart;
                            _stack[slot] = _stack.Peek();
                            break;
                        }
                    case OpCode.DEFINE_MODULE_VAR:
                        {
                            int index = ReadUShort(ref frame);
                            frame.Function.Module.Variables[index] = _stack.Pop();
                            break;
                        }
                    case OpCode.GET_MODULE_VAR:
                        {
                            int index = ReadUShort(ref frame);
                            Value val = frame.Function.Module.Variables[index];
                            if (val.IsUndefined)
                            {
                                RuntimeError($"Use of unassigned module variable '{val.AsString}'");
                                return InterpretResult.RuntimeError;
                            }
                            else
                            {
                                _stack.Push(val);
                            }

                            break;
                        }
                    case OpCode.SET_MODULE_VAR:
                        {
                            int index = ReadUShort(ref frame);
                            Value val = frame.Function.Module.Variables[index];
                            if (val.IsUndefined)
                            {
                                RuntimeError($"Use of unassigned module variable '{val.AsString}'");
                                return InterpretResult.RuntimeError;
                            }
                            else
                            {
                                frame.Function.Module.Variables[index] = _stack.Peek(0);
                            }
                            break;
                        }
                    case OpCode.GET_PROPERTY:
                        {
                            if (!_stack.Peek().IsInstance)
                            {
                                RuntimeError("Only instances have properties.");
                                return InterpretResult.RuntimeError;
                            }

                            ClassInstance instance = _stack.Peek().AsInstance;
                            string propertyName = ReadConstant16(ref frame).AsString;
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
                                RuntimeError("Only instances have fields.");
                                return InterpretResult.RuntimeError;
                            }

                            ClassInstance instance = _stack.Peek(1).AsInstance;
                            string fieldName = ReadConstant16(ref frame).AsString;

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
                                RuntimeError($"undefined property '{fieldName}'");
                                return InterpretResult.RuntimeError;
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
                                            RuntimeError("Expects a number to use as an index.");
                                            return InterpretResult.RuntimeError;
                                        }
                                        double index = _stack.Peek().AsDouble;
                                        _stack.Discard(2);
                                        _stack.Push(arrayInstance.Values[(int)index]);
                                        break;
                                    }
                                // todo map
                                default:
                                    RuntimeError("Only arrays and maps can use index.");
                                    return InterpretResult.RuntimeError;
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
                                            RuntimeError("Expects a number to use as an index.");
                                            return InterpretResult.RuntimeError;
                                        }
                                        double index = _stack.Peek(1).AsDouble;

                                        if (index >= arrayInstance.Values.Count)
                                        {
                                            RuntimeError("Array index out of bounds.");
                                            return InterpretResult.RuntimeError;
                                        }

                                        ref Value val = ref _stack.Peek();
                                        _stack.Discard(3);
                                        arrayInstance.Values[(int)index] = val; 
                                        _stack.Push(val);
                                        break;
                                    }
                                    // todo map
                                    default :
                                    RuntimeError("Only arrays and maps can use index.");
                                    return InterpretResult.RuntimeError;
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
                    case OpCode.MOD:
                        if (!BinaryOperator(instruction))
                        {
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    case OpCode.NOT:
                        _stack.Push(!_stack.Pop());
                        break;
                    case OpCode.NEGATE:
                        if (!_stack.Peek().IsNumber)
                        {
                            RuntimeError("Operand must be a number.");
                            return InterpretResult.RuntimeError;
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
                            string methodName = ReadConstant16(ref frame).AsString;
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
                                return InterpretResult.Success;
                            }

                            _currentInstructions = _callFrames.Peek().Function.Chunk.Instructions;
                            _currentConstants = _callFrames.Peek().Function.Chunk.Constants;

                            _stack.Discard(_stack.Count - frame.StackStart);
                            _stack.Push(result);
                            break;
                        }
                    case OpCode.DEFINE_CLASS:
                        {
                            string className = ReadConstant16(ref frame).AsString;
                            _stack.Push(new Value(new InternalClass(className)));
                            break;
                        }
                    case OpCode.CLASS_METHOD:
                        {
                            string methodName = ReadConstant16(ref frame).AsString;
                            ref readonly Value method = ref _stack.Peek();
                            InternalClass internalClass = _stack.Peek(1).AsClass;
                            internalClass.Methods[methodName] = method;
                            _stack.Pop();
                            break;
                        }
                    case OpCode.IMPORT_MODULE:
                        {
                            string moduleName = ReadConstant16(ref frame).AsString; 
                            _stack.Push(ImportModule(moduleName));

                            if (_stack.Peek().IsNull)
                            {
                                return InterpretResult.RuntimeError;
                            }

                            // If we get a function, call it to execute the module body.
                            if (_stack.Peek().IsFunction)
                            {
                                CallInternalFunction(_stack.Peek().AsFunction, 0);
                            }
                            else if(_stack.Peek().IsModule) 
                            {
                                // The module has already been loaded. Remember it so we can import
                                // variables from it if needed.
                                LastLoadedModule = _stack.Peek().AsModule;
                            }
                            break;
                        }
                    case OpCode.IMPORT_ALL_VARIABLE:
                        {
                            Debug.Assert(LastLoadedModule != null);
                            Module module = new Module(LastLoadedModule.Name);
                            //  Add to the new module except for the core module.
                            foreach (var variableIndex in LastLoadedModule.VariableIndexes)
                            {
                                string name = variableIndex.Key;
                                if (LoadedModules[string.Empty].VariableIndexes.ContainsKey(name)) 
                                {
                                    continue;
                                }
                                int index = variableIndex.Value;

                                module.Variables.Add(LastLoadedModule.Variables[index]);
                                module.VariableIndexes[name] = module.Variables.Count;
                            }
                            _stack.Push(new Value(module)); 
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
        private Value ReadConstant8(ref CallFrame callFrame)
        {
            return _currentConstants[ReadByte(ref callFrame)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Value ReadConstant16(ref CallFrame callFrame)
        {
            return _currentConstants[ReadUShort(ref callFrame)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool BinaryOperator(OpCode op)
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
                        RuntimeError("Operands must be numbers.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
                case OpCode.LESS:
                    res = a < b;
                    if (res.IsNull)
                    {
                        RuntimeError("Operands must be numbers.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
                case OpCode.ADD:
                    res = a + b;
                    if (res.IsNull)
                    {
                        RuntimeError("Operands must be numbers or left operand is string.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
                case OpCode.SUBTRACT:
                    res = a - b;
                    if (res.IsNull)
                    {
                        RuntimeError("Operands must be numbers.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
                case OpCode.MULTIPLY:
                    res = a * b;
                    if (res.IsNull)
                    {
                        RuntimeError("Operands must be numbers.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
                case OpCode.DIVIDE:
                    res = a / b;
                    if (res.IsNull)
                    {
                        RuntimeError("Operands must be numbers.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
                case OpCode.MOD:
                    res = a % b;
                    if (res.IsNull) 
                    {
                        RuntimeError("Operands must be numbers.");
                        return false;
                    }
                    _stack.Push(res);
                    break;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    RuntimeError("Can only call functions and classes.");
                    break;// Non-callable object type.
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Invoke(string methodName, int argCount)
        {
            ref Value receiver = ref _stack.Peek(argCount);
            if (!receiver.IsInstance)
            {
                RuntimeError("Only instances have methods.");
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
                    RuntimeError($"Undefined method {methodName}");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeFromClass(InternalClass internalClass, string methodName, int argCount)
        {
            if (!internalClass.Methods.TryGetValue(methodName, out var method))
            {
                RuntimeError($"Undefined method {methodName}");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallInternalFunction(in Function function, int argCount, in InternalClass? scriptClass = null)
        {
            if (argCount != function.Arity)
            {
                RuntimeError($"Expected {function.Arity} arguments but got {argCount}.");
                return;
            }

            if (_callFrames.Count == FRAME_MAX)
            {
                RuntimeError("Stack overflow.");
                return;
            }

            CallFrame callFrame = new(function, _stack.Count - argCount - 1) { Class = scriptClass };
            _currentInstructions = callFrame.Function.Chunk.Instructions;
            _currentConstants = callFrame.Function.Chunk.Constants;
            _callFrames.Push(callFrame);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                RuntimeError($"Expected 0 arguments but got {argCount}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateBindMethod(InternalClass internalClass, string methodName)
        {
            if (!internalClass.Methods.TryGetValue(methodName, out var method))
            {
                RuntimeError($"Undefined method '{methodName}'");
            }
            else
            {
                BoundMethod boundMethod = new(_stack.Peek(), method);
                _stack[_stack.Count - 1] = new Value(boundMethod);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        private Value ImportModule(string moduleName)
        {
            // If the module is already loaded, we don't need to do anything.
            if (LoadedModules.TryGetValue(moduleName, out var module)) 
            {
                return new Value(module);
            }

            if (Config.LoadMuduleFunction == null)
            {
                RuntimeError($"Could not load module '{moduleName}'. Unable to find a method to load the module.");
                return Value.NUll;
            }

            string src = Config.LoadMuduleFunction.Invoke(moduleName);
            Function? compiled = Compiler.Compile(this, moduleName, src);
            if (compiled == null)
            {
                RuntimeError($"Could not compile module {moduleName}.");
                return Value.NUll;
            }
            LastLoadedModule = compiled.Module;
            return new Value(compiled);
        }

        private void AppendCallFrame()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RuntimeError(string message)
        {
            if (Config.PrintErrorFn == null) return;

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
            //throw new RuntimeException(errorMsg);
            Function currentFunction = _callFrames.Peek().Function;
            int currentLine = currentFunction.Chunk.GetLineNumber(_callFrames.Peek().Ip - 1);
            Config.PrintErrorFn.Invoke(ErrorType.RuntimeError, string.Empty, currentLine, errorMsg);
        }
        #endregion

        #region External call 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
