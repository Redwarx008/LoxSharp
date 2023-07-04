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

        Pop,
        Define_Global,
        Get_Global,

        EQUAL,
        GREATER,
        LESS,

        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        NOT,

        NEGATE,

        Print,
        RETURN
    }
}
