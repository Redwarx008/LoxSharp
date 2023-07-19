namespace LoxSharp.Core
{
    internal enum OpCode : byte
    {
        CONSTANT_8,
        CONSTANT_16,

        NULL,
        TRUE,
        FALSE,

        POP,
        GET_LOCAL,
        SET_LOCAL,
        DEFINE_MODULE_VAR,
        GET_GLOBAL,
        SET_GLOBAL,
        GET_PROPERTY,
        SET_PROPERTY,
        GET_INDEX,
        SET_INDEX,

        IMPORT_MODULE,
        IMPORT_VARIABLE,
        IMPORT_ALL_VARIABLE,

        EQUAL,
        GREATER,
        LESS,

        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,
        NOT,

        NEGATE,

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
