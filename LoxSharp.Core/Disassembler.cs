using System.Text;

namespace LoxSharp.Core
{
    internal class Disassembler
    {
        public static Disassembler Instance => _disassembler;

        private static Disassembler _disassembler = new();
        private StringBuilder _sb = new();

        private Disassembler()
        {

        }

        public string GetText()
        {
            string txt = _sb.ToString();
            _sb.Clear();
            return txt;
        }

        public void DisassembleFunction(Function function, List<Value> globalValues)
        {
            _sb.Clear();
            _sb.Append($"== {function.Name} ==\n");
            for (int offset = 0; offset < function.Chunk.Instructions.Count;)
            {
                offset = DisassembleInstruction(function.Chunk, offset, globalValues);
            }
        }

        public void DisassembleStack(ValueStack<Value> stack)
        {
            _sb.Append("          ");
            for (int i = 0; i < stack.Count; ++i)
            {
                _sb.Append($"[{stack[i]}]");
            }
            _sb.Append('\n');
        }

        public int DisassembleInstruction(Chunk chunk, int offset, List<Value> globalValues)
        {
            // offset
            _sb.Append(offset.ToString("0000"));

            // line number
            if (offset > 0 && chunk.GetLineNumber(offset) == chunk.GetLineNumber(offset - 1))
            {
                _sb.Append("   | ");
            }
            else
            {
                _sb.Append($"{chunk.GetLineNumber(offset),4} ");
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
                case OpCode.GET_GLOBAL:
                case OpCode.SET_GLOBAL:
                case OpCode.DEFINE_MODULE_VAR:
                    return GlobalValueInstruction(instruction, chunk, offset, globalValues);
                case OpCode.GET_PROPERTY:
                case OpCode.SET_PROPERTY:
                case OpCode.CLASS:
                case OpCode.CLASS_METHOD:
                    return ConstantInstruction(instruction, chunk, offset);
                case OpCode.JUMP:
                case OpCode.JUMP_IF_FALSE:
                    return JumpInstruction(instruction, 1, chunk, offset);
                case OpCode.LOOP:
                    return JumpInstruction(instruction, -1, chunk, offset);
                default:
                    _sb.Append($"Unknown opcode {instruction.ToString()}");
                    return offset + 1;
            }
        }

        private int ConstantInstruction(OpCode instruction, Chunk chunk, int offset)
        {
            byte constant = chunk.Instructions[offset + 1];
            _sb.Append($"{instruction.ToString(),-16}{constant,4}");
            _sb.Append($"    {chunk.Constants[constant].ToString()}");
            _sb.Append('\n');
            return offset + 2;
        }

        private int InvokeInstruction(OpCode instruction, Chunk chunk, int offset)
        {
            byte constant = chunk.Instructions[offset + 1];
            byte argCount = chunk.Instructions[offset + 2];
            _sb.Append($"{instruction.ToString(),-16}({argCount,4}) args ");
            _sb.Append($"{constant,4}\n");
            return offset + 3;
        }

        private int SimpleInstruction(OpCode instruction, int offset)
        {
            _sb.Append(instruction.ToString());
            _sb.Append('\n');
            return offset + 1;
        }

        private int ByteInstruction(OpCode instruction, Chunk chunk, int offset)
        {
            byte slot = chunk.Instructions[offset + 1];
            _sb.Append($"{instruction.ToString(),-16}{slot,4}\n");
            return offset + 2;
        }

        private int JumpInstruction(OpCode instruction, int direction, Chunk chunk, int offset)
        {
            var high = chunk.Instructions[offset + 1] << 8;
            var low = chunk.Instructions[offset + 2];
            ushort jump = (ushort)(high | low);

            _sb.Append($"{instruction.ToString(),-16}{offset,4} -> {offset + 3 + direction * jump}\n");
            return offset + 3;
        }

        private int GlobalValueInstruction(OpCode instruction, Chunk chunk, int offset, List<Value> globalValues) 
        {
            byte index = chunk.Instructions[offset + 1];
            _sb.Append($"{instruction.ToString(),-16}{index,4}");
            _sb.Append($"    {globalValues[index].ToString()}");
            _sb.Append('\n');
            return offset + 2;
        }
    }
}
