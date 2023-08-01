namespace SharpES.Core
{
    public enum InterpretResult
    {
        Success,
        CompileError,
        RuntimeError,
    }
    public class ScriptEngine
    {
        public static InterpretResult Run(VM vm, string src)
        {
            var compiledScript = Compiler.Compile(vm, "main", src);
            if (compiledScript == null)
            {
                return InterpretResult.CompileError;
            }
            return vm.Interpret(compiledScript);
        }

        /// <summary>
        ///  Set the value of a module variable, overwrite if the value already exists.
        /// </summary>
        /// <returns> If set successfully, return <see langword="true"/>. </returns>
        public static bool SetModuleVariable(VM vm, string moduleName, string varName, Value val)
        {
            if (vm.LoadedModules.TryGetValue(moduleName, out var module))
            {
                module.SetVariable(varName, val);   
                return true;    
            }
            else
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"Module named {moduleName} was not found.");
                return false;
            }
        }

        /// <summary>
        /// Get the value of a module variable.
        /// </summary>
        /// <returns>return <see langword="null"/> if not found. </returns>
        public static Value? GetModuleVariable(VM vm, string moduleName, string varName)
        {
            if (vm.LoadedModules.TryGetValue(moduleName, out var module))
            {
                if (module.VariableIndexes.TryGetValue(varName, out int index))
                {
                    return module.Variables[index]; 
                }
                else
                {
                    vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                        $"Can't find a variable named {varName} in {moduleName}.");
                    return null;
                }
            }
            else
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"Module named {moduleName} was not found.");
                return null;
            }
        }

        public static Value? Call(VM vm, string moduleName, string funcName, params Value[] args)
        {
            if (vm.LoadedModules.TryGetValue(moduleName, out var module))
            {
                if (module.VariableIndexes.TryGetValue(funcName, out int index))
                {
                    return vm.CallFunctionFromForeign(module.Variables[index], args);
                }
                else
                {
                    vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                        $"Can't find a function named {funcName}.");
                    return null;
                }
            }
            else
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"Module named {moduleName} was not found.");
                                return null;
            }
        }

        public static Value? Call(VM vm, in Value callee, params Value[] args)
        {
            return vm.CallFunctionFromForeign(callee, args);
        }

        public static void RegisterForeignFunction(VM vm, ForeignFunction function)
        {
            SetModuleVariable(vm, string.Empty, function.Name, new Value(function));    
        }

        public static void RegisterForeignClass(VM vm, Class @class)
        {
            SetModuleVariable(vm, string.Empty, @class.Name, new Value(@class));
        }
    }
}
