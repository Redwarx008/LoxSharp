using System.Text;

namespace LoxSharp.Core
{
    internal class Disassembler
    {
        public static void DisassembleFunction(Function function)
        {
            Console.Write($"== {function.Name} ==\n");
            for (int offset = 0; offset < function.Chunk.Instructions.Count;)
            {
                offset = DisassembleInstruction(function.Chunk, offset, function.Module.Variables);
            }
        }

        public static void DisassembleStack(ValueStack<Value> stack)
        {
            Console.Write("          ");
            for (int i = 0; i < stack.Count; ++i)
            {
                Console.Write($"[{stack[i]}]");
            }
            Console.Write('\n');
        }

        public static int DisassembleInstruction(Chunk chunk, int offset, List<Value> variables)
        {
            // offset
            Console.Write(offset.ToString("0000"));

            // line number
            if (offset > 0 && chunk.GetLineNumber(offset) == chunk.GetLineNumber(offset - 1))
            {
                Console.Write("   | ");
            }
            else
            {
                Console.Write($"{chunk.GetLineNumber(offset),4} ");
            }

            OpCode instruction = (OpCode)chunk.Instructions[offset];
            switch (instruction)
            {
                case OpCode.CONSTANT_8:
                    return ConstantInstruction(instruction, chunk, offset);
                case OpCode.NULL:
                case OpCode.TRUE:
                case OpCode.FALSE:
                case OpCode.POP:
                case OpCode.EQUAL:
                case OpCode.GREATER:
                case OpCode.LESS:
                case OpCode.ADD:
                case OpCode.SUBTRACT:
                case OpCode.MULTIPLY:
                case OpCode.DIVIDE:
                case OpCode.NOT:
                case OpCode.NEGATE:
                case OpCode.RETURN:
                case OpCode.SET_INDEX:
                case OpCode.GET_INDEX:
                    return SimpleInstruction(instruction, offset);
                case OpCode.GET_LOCAL:
                case OpCode.SET_LOCAL:
                case OpCode.CALL:
                    return ByteInstruction(instruction, chunk, offset);
                case OpCode.INVOKE:
                    return InvokeInstruction(instruction, chunk, offset);
                case OpCode.GET_MODULE_VAR:
                case OpCode.SET_MODULE_VAR:
                case OpCode.DEFINE_MODULE_VAR:
                    return ModuleVariableInstruction(instruction, chunk, offset, variables);
                case OpCode.GET_PROPERTY:
                case OpCode.SET_PROPERTY:
                case OpCode.DEFINE_CLASS:
                case OpCode.CLASS_METHOD:
                    return ConstantInstruction(instruction, chunk, offset);
                case OpCode.JUMP:
                case OpCode.JUMP_IF_FALSE:
                    return JumpInstruction(instruction, 1, chunk, offset);
                case OpCode.LOOP:
                    return JumpInstruction(instruction, -1, chunk, offset);
                default:
                    Console.Write($"Unknown opcode {instruction.ToString()}");
                    return offset + 1;
            }
        }

        private static int ConstantInstruction(OpCode instruction, Chunk chunk, int offset)
        {
            byte constant = chunk.Instructions[offset + 1];
            Console.Write($"{instruction.ToString(),-16}{constant,4}");
            Console.Write($"    {chunk.Constants[constant].ToString()}");
            Console.Write('\n');
            return offset + 2;
        }

        private static int InvokeInstruction(OpCode instruction, Chunk chunk, int offset)
        {
            byte constant = chunk.Instructions[offset + 1];
            byte argCount = chunk.Instructions[offset + 2];
            Console.Write($"{instruction.ToString(),-16}({argCount,4}) args ");
            Console.Write($"{constant,4}\n");
            return offset + 3;
        }

        private static int SimpleInstruction(OpCode instruction, int offset)
        {
            Console.Write(instruction.ToString());
            Console.Write('\n');
            return offset + 1;
        }

        private static int ByteInstruction(OpCode instruction, Chunk chunk, int offset)
        {
            byte slot = chunk.Instructions[offset + 1];
            Console.Write($"{instruction.ToString(),-16}{slot,4}\n");
            return offset + 2;
        }

        private static int JumpInstruction(OpCode instruction, int direction, Chunk chunk, int offset)
        {
            var high = chunk.Instructions[offset + 1] << 8;
            var low = chunk.Instructions[offset + 2];
            ushort jump = (ushort)(high | low);

            Console.Write($"{instruction.ToString(),-16}{offset,4} -> {offset + 3 + direction * jump}\n");
            return offset + 3;
        }

        private static int ModuleVariableInstruction(OpCode instruction, Chunk chunk, int offset, List<Value> variables) 
        {
            byte index = chunk.Instructions[offset + 1];
            Console.Write($"{instruction.ToString(),-16}{index,4}");
            Console.Write($"    {variables[index].ToString()}");
            Console.Write('\n');
            return offset + 2;
        }
    }
}
