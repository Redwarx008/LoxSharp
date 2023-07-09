using LoxSharp.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void DisassembleChunk(Chunk chunk, string name)
        {
            _sb.Clear();    
            _sb.Append($"== {name} ==\n");
            for(int offset = 0; offset < chunk.Instructions.Count;)
            {
                offset = DisassembleInstruction(chunk, offset);
            }
        }

        public void DisassembleStack(ValueStack<Value> stack)
        {
            _sb.Append("          ");
            for(int i = 0; i < stack.Count; ++i)
            {
                _sb.Append($"[{stack[i]}]");
            }
            _sb.Append('\n');
        }

        public int DisassembleInstruction(Chunk chunk, int offset)
        {
            // offset
            _sb.Append(offset.ToString("0000"));

            // line number
            if(offset > 0 && chunk.LineNumbers[offset] == chunk.LineNumbers[offset - 1])
            {
                _sb.Append("   | ");
            }
            else
            {
                _sb.Append($"{chunk.LineNumbers[offset], 4} ");
            }

            OpCode instruction = ( OpCode)chunk.Instructions[offset];
            switch (instruction) 
            {
                case OpCode.CONSTANT:
                    return ConstantInstruction(instruction, chunk, offset);
                case OpCode.NIL:
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
                case OpCode.PRINT:
                case OpCode.RETURN:
                    return SimpleInstruction(instruction, offset);
                case OpCode.GET_LOCAL:
                case OpCode.SET_LOCAL:
                case OpCode.CALL:
                    return ByteInstruction(instruction, chunk, offset);
                case OpCode.GET_GLOBAL:
                case OpCode.SET_GLOBAL:
                case OpCode.DEFINE_GLOBAL:
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
            _sb.Append($"{instruction.ToString(), -16}{constant, 4}");
            _sb.Append($"{chunk.Constants[constant].ToString(), 10}");
            _sb.Append('\n');
            return offset + 2;  
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
            _sb.Append($"{instruction.ToString(), -16}{slot, 4}\n");
            return offset + 2;  
        }

        private int JumpInstruction(OpCode instruction, int direction, Chunk chunk, int offset)
        {
            var high = chunk.Instructions[offset + 1] << 8;
            var low = chunk.Instructions[offset + 2];
            ushort jump = (ushort)(high | low);

            _sb.Append($"{instruction.ToString(), -16}{offset, 4} -> {offset + 3 + direction * jump}\n");
            return offset + 3;
        }
    }
}
