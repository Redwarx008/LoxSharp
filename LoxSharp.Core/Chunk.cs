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
    }
}
