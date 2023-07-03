using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class Chunk
    {
        public List<byte> Instructions { get; private set; } = new List<byte>();

        public List<Value> Constants { get; private set; } = new List<Value>(); 

        private List<int> _lines = new List<int>();

        public void WriteByte(byte b, int line)
        {
            Instructions.Add(b);
            _lines.Add(line);
        }

        /// <summary>
        /// Add a value to the constant list and return its index 
        /// </summary>
        public int AddConstant(Value val)
        {
            Constants.Add(val);
            return Constants.Count - 1;
        }
    }
}
