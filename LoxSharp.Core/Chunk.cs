using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class Chunk
    {
        private List<byte> _instructions = new List<byte>();

        private List<Value> _constants = new List<Value>(); 

        private List<int> _lines = new List<int>();

        public void WriteByte(byte b, int line)
        {
            _instructions.Add(b);
            _lines.Add(line);
        }

        /// <summary>
        /// Add a value to the constant list and return its index 
        /// </summary>
        public int AddConstant(Value val)
        {
            _constants.Add(val);
            return _constants.Count - 1;
        }
    }
}
