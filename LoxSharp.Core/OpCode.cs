using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal enum OpCode : byte
    {
        CONSTANT,

        NIL,
        TRUE,
        FALSE,

        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        NOT,

        NEGATE,

        RETURN
    }
}
