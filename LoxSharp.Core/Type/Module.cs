using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class Module
    {
        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The currently defined top-level variables.
        /// </summary>
        public List<Value> Variables { get; private set; }

        /// <summary>
        /// The names of all module variables. Indexes here directly correspond to entries in [variables].
        /// </summary>
        public Dictionary<string, int> VariableIndexes { get; private set; }
        public Module(string name)
        {
            Name = name;
            Variables = new List<Value>();
            VariableIndexes = new Dictionary<string, int>();
        }

        /// <summary>
        /// Add top-level variables to the module 
        /// </summary>
        /// <param name="vm"> Used to report errors </param>
        public void AddVariable(VM vm, string varName, Value val)
        {
            if (VariableIndexes.ContainsKey(varName))
            {
                vm.Config.PrintErrorFn?.Invoke(ErrorType.OtherError, Name, -1, 
                    $"A variable named {varName} already exists.");
                return;
            }

            int index = Variables.Count;
            VariableIndexes[varName] = index;
            Variables.Add(val);
        }
    }
}
