using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace LoxSharp.Core
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
        public static bool SetModuleVariable(VM vm, string varName, Value val) 
        {
            if (vm.LastLoadedModule == null) 
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"The script has not been run yet.");
                return false;
            }
            
            vm.LastLoadedModule.SetVariable(varName, val);
            return true;
        }

        /// <summary>
        /// Get the value of a module variable.
        /// </summary>
        /// <returns>return <see langword="null"/> if not found. </returns>
        public static Value? GetModuleVariable(VM vm, string varName)
        {
            if (vm.LastLoadedModule == null)
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"The script has not been run yet.");
                return null;
            }

            if (vm.LastLoadedModule.VariableIndexes.TryGetValue(varName, out int index)) 
            {
                return vm.LastLoadedModule.Variables[index];    
            }
            else
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"Can't find a variable named {varName}.");
                return null;
            }
        }

        public static Value? Call(VM vm, string funcName, params Value[] args)
        {
            if (vm.LastLoadedModule == null)
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, string.Empty, -1,
                    $"The script has not been run yet.");
                return null;
            }

            Module module = vm.LastLoadedModule;
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

        public static Value? Call(VM vm, in Value callee, params Value[] args) 
        {
            return vm.CallFunctionFromForeign(callee, args);
        }
    }
}
