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

        POP,
        GET_LOCAL,
        SET_LOCAL,
        DEFINE_GLOBAL,
        GET_GLOBAL,
        SET_GLOBAL,
        GET_PROPERTY,
        SET_PROPERTY,

        EQUAL,
        GREATER,
        LESS,

        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        NOT,

        NEGATE,

        PRINT,

        JUMP_IF_FALSE,
        JUMP,
        LOOP,

        CALL,
        INVOKE,
        CLASS,
        CLASS_METHOD,
        RETURN
    }
}
