using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LoxSharp.Core
{
    internal struct CallFrame
    {
        public Class? Class { get; set; } = null;
        public Function Function { get; private set; }
        public int Ip { get; set; } = 0;
        public int StackStart { get; private set; }

        public CallFrame(Function function, int stackStart)
        {
            Function = function;
            StackStart = stackStart;
        }
    }

    // todo : add coroutine support?
    //internal class Coroutine
    //{
    //    public ValueStack<CallFrame> CallFrames { get; private set; }
    //    public ValueStack<Value> Stack { get; private set; }
    //    /// <summary> 
    //    /// The fiber that ran this one. If this fiber is yielded, control will resume to this one.
    //    /// </summary>
    //    public Coroutine? Caller { get; set; }

    //    public Coroutine(Function function)
    //    {
    //        CallFrames = new ValueStack<CallFrame>(VM.FRAME_MAX);
    //        Stack = new ValueStack<Value>(VM.STACK_MAX);

    //        CallFrames.Push(new CallFrame(function, Stack.Count));
    //        // The first slot always holds the function.
    //        Stack.Push(new Value(function));
    //    }
    //}

    public class VM
    {
        internal const int STACK_MAX = 256;
        internal const int FRAME_MAX = 64;

        private ValueStack<CallFrame> _callFrames = new(FRAME_MAX);
        private ValueStack<Value> _stack = new(FRAME_MAX * STACK_MAX);

        private List<byte> _currentInstructions = null!;
        private List<Value> _currentConstants = null!;

        private Value? _lastReturn;

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

        internal InterpretResult Interpret(Function compiledFunc)
        {
            _stack.Push(new Value(compiledFunc));

            CallFrame callframe = new(compiledFunc, 0);
            _currentInstructions = callframe.Function.Chunk.Instructions;
            _currentConstants = callframe.Function.Chunk.Constants;
            _callFrames.Push(callframe);

            InterpretResult interpretResult = Run();
            _lastReturn = null;

            return interpretResult;
        }

        private void InitCoreModule()
        {
            Module coreModule = new Module(string.Empty);
            LoadedModules[coreModule.Name] = coreModule;
            coreModule.SetVariable(nameof(Array), new Value(new Array()));

            ForeignFunction printFunc = new("Print", (args) =>
            {
                if (Config.WriteFunction == null) return Value.NUll;
                Config.WriteFunction.Invoke(args[0].ToString());
                return Value.NUll;
            });
            coreModule.SetVariable(printFunc.Name, new Value(printFunc));

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
                            if (_stack.Peek().IsModule)
                            {
                                Module module = _stack.Peek().AsModule;
                                string varName = ReadConstant16(ref frame).AsString;
                                if (module.VariableIndexes.TryGetValue(varName, out int index))
                                {
                                    _stack[_stack.Count - 1] = module.Variables[index];
                                }
                                else
                                {
                                    RuntimeError($"The name '{varName}' does not exist in the {module.Name} module.");
                                    return InterpretResult.RuntimeError;
                                }
                                break;
                            }

                            if (_stack.Peek().IsInstance)
                            {
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

                            if (_stack.Peek().IsClass)
                            {
                                Class @class = _stack.Peek().AsClass;
                                string methodName = ReadConstant16(ref frame).AsString;
                                if (@class.StaticMethod.TryGetValue(methodName, out var value))
                                {
                                    BoundMethod boundMethod = new(_stack.Peek(), value);
                                    _stack[_stack.Count - 1] = new Value(boundMethod);
                                    break;
                                }
                                else
                                {
                                    RuntimeError($"Cannot find a static method named {methodName}.");
                                }
                            }

                            RuntimeError("The operation object must be one of module, class, or class instance.");
                            return InterpretResult.RuntimeError;
                        }
                    case OpCode.SET_PROPERTY:
                        {
                            if (_stack.Peek(1).IsModule)
                            {
                                Module module = _stack.Peek(1).AsModule;
                                string varName = ReadConstant16(ref frame).AsString;

                                if (module.VariableIndexes.TryGetValue(varName, out int index))
                                {
                                    module.Variables[index] = _stack.Peek();
                                    // remove the second element from the stack.
                                    Value val = _stack.Pop();
                                    _stack.Pop();
                                    _stack.Push(val);
                                }
                                else
                                {
                                    RuntimeError($"The name '{varName}' does not exist in the {module.Name} module.");
                                    return InterpretResult.RuntimeError;
                                }
                                break;
                            }

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
                                _lastReturn = _stack.Pop();
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
                            _stack.Push(new Value(new Class(className)));
                            break;
                        }
                    case OpCode.DEFINE_METHOD:
                        {
                            string methodName = ReadConstant16(ref frame).AsString;
                            bool isStatic = ReadConstant16(ref frame).AsBool;
                            ref readonly Value method = ref _stack.Peek();
                            Class class_ = _stack.Peek(1).AsClass;
                            if (!isStatic)
                            {
                                class_.Methods[methodName] = method;
                            }
                            else
                            {
                                class_.StaticMethod[methodName] = method;
                            }
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
                                CallFunction(_stack.Peek().AsFunction, 0);
                            }
                            else if(_stack.Peek().IsModule) 
                            {
                                // The module has already been loaded. Remember it so we can import
                                // variables from it if needed.
                                LastLoadedModule = _stack.Peek().AsModule;
                            }
                            break;
                        }
                    case OpCode.END_MODULE:
                        {
                            LastLoadedModule = _callFrames.Peek().Function.Module;
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

                                module.VariableIndexes[name] = module.Variables.Count;
                                module.Variables.Add(LastLoadedModule.Variables[index]);
                            }
                            _stack.Push(new Value(module)); 
                            break;
                        }
                    case OpCode.IMPORT_VARIABLE:
                        {
                            Debug.Assert(LastLoadedModule != null, "Should have already imported module.");
                            string sourceVarName = ReadConstant16(ref frame).AsString;
                            if (LastLoadedModule.VariableIndexes.TryGetValue(sourceVarName, out int variableIndex)) 
                            {
                                _stack.Push(LastLoadedModule.Variables[variableIndex]);
                            }
                            else
                            {
                                RuntimeError($"The name '{sourceVarName}' does not exist in the {LastLoadedModule.Name} module.");
                                return InterpretResult.RuntimeError;
                            }
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
        private void CallValue(in Value callee, int argCount, Class? enclosingClass = null)
        {
            switch (callee.Type)
            {
                case Value.ValueType.Function:
                    CallFunction(callee.AsFunction, argCount, enclosingClass);
                    break;
                case Value.ValueType.BoundMethod:
                    CallBoundMethod(callee.AsBoundMethod, argCount);
                    break;
                case Value.ValueType.Class:
                    CreateInstance(callee.AsClass, argCount);
                    break;
                case Value.ValueType.ForeignFunction:
                    CallForeignFunction(callee.AsForeignFunction, argCount);  
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

            if (receiver.IsClass)
            {
                Class @class = receiver.AsClass;    
                if (@class.StaticMethod.TryGetValue(methodName, out Value staticMethod))
                {
                    switch (staticMethod.Type)
                    {
                        case Value.ValueType.Function:
                            CallFunction(staticMethod.AsFunction, argCount, @class);
                            break;
                        case Value.ValueType.ForeignFunction:
                            CallForeignFunction(staticMethod.AsForeignFunction, argCount);
                            break;
                    }
                }
                return;
            }

            if (!receiver.IsInstance)
            {
                RuntimeError("(Static)Methods can only be called through class or instance.");
            }
            ClassInstance instance = receiver.AsInstance;
            if (instance.Fields.TryGetValue(methodName, out var field))
            {
                _stack.Peek(argCount) = field;
                CallValue(field, argCount, instance.Class);
            }
            else
            {
                if (instance.Class.Methods.TryGetValue(methodName, out var method))
                {
                    switch (method.Type)
                    {
                        case Value.ValueType.Function:
                            CallFunction(method.AsFunction, argCount, instance.Class);
                            break;
                        case Value.ValueType.ForeignMethod:
                            CallForeignMethod(instance, method.AsForeignMethod, argCount);
                            break;
                    }
                }
                else
                {
                    RuntimeError($"Undefined method {methodName}");
                }
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void InvokeFromClass(Class internalClass, string methodName, int argCount)
        //{
        //    if (!internalClass.Methods.TryGetValue(methodName, out var method))
        //    {
        //        RuntimeError($"Undefined method {methodName}");
        //    }
        //    else
        //    {
        //        switch (method.Type) 
        //        {
        //            case Value.ValueType.Function:
        //                CallInternalFunction(method.AsFunction, argCount, internalClass);
        //                break;
        //            case Value.ValueType.ForeignMethod:
        //                CallHostMethod(method.AsForeignMethod, argCount);
        //                break;
        //        }
        //    }
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CallFunction(in Function function, int argCount, in Class? enclosingClass = null)
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

            CallFrame callFrame = new(function, _stack.Count - argCount - 1) { Class = enclosingClass };
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
                case Value.ValueType.Function:
                    CallFunction(boundMethod.Function.AsFunction, argCount);
                    break;
                case Value.ValueType.ForeignMethod:
                    CallForeignMethod(boundMethod.Receiver.AsInstance, boundMethod.Function.AsForeignMethod, argCount);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateInstance(Class class_, int argCount)
        {

            ClassInstance instance = class_.CreateInstance();
 
            _stack.Peek(argCount) = new Value(instance);

            // call initializer
            if (class_.Methods.TryGetValue("init", out var method))
            {
                switch (method.Type)
                {
                    case Value.ValueType.Function:
                        CallFunction(method.AsFunction, argCount, class_);
                        break;
                    case Value.ValueType.ForeignMethod:
                        CallForeignMethod(instance, method.AsForeignMethod, argCount);
                        break;
                }
            }
            else if (argCount != 0)
            {
                RuntimeError($"Expected 0 arguments but got {argCount}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateBindMethod(Class class_, string methodName)
        {
            if (!class_.Methods.TryGetValue(methodName, out var method))
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
        private void CallForeignFunction(ForeignFunction function, int argCount)
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
        private void CallForeignMethod(ClassInstance instance, ForeignMethod method, int argCount)
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

            if (Config.LoadModuleFunction == null)
            {
                RuntimeError($"Could not load module '{moduleName}'. Unable to find a method to load the module.");
                return Value.NUll;
            }

            string src = Config.LoadModuleFunction.Invoke(moduleName);
            Function? compiled = Compiler.Compile(this, moduleName, src);
            if (compiled == null)
            {
                RuntimeError($"Could not compile module {moduleName}.");
                return Value.NUll;
            }
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
        internal Value? CallFunctionFromForeign(in Value callee, params Value[] args)
        {
            _stack.Push(callee);    

            for(int i = 0; i < args.Length; ++i)
            {
                _stack.Push(args[i]);   
            }

            CallValue(callee, args.Length);
            Run();
            return _lastReturn;
        }
        #endregion
    }
}
