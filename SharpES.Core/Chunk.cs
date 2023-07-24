namespace SharpES.Core
{
    internal class Chunk
    {
        private struct LineStart
        {
            public int Offset { get; private set; }
            public int LineNumber { get; private set; }

            public LineStart(int offset, int lineNumber)
            {
                Offset = offset;
                LineNumber = lineNumber;
            }
        }

        public List<byte> Instructions { get; private set; } = new List<byte>();

        public List<Value> Constants { get; private set; } = new List<Value>();

        //public IReadOnlyList<LineStart> LineNumbers => _lineNumbers;

        private List<LineStart> _lineStarts = new List<LineStart>();

        public void WriteByte(byte b, int line)
        {
            Instructions.Add(b);

            if (_lineStarts.Count > 0 && _lineStarts[^1].LineNumber == line)
            {
                return;
            }

            _lineStarts.Add(new LineStart(Instructions.Count - 1, line));
        }

        /// <summary>
        /// Add a value to the constant list and return its index 
        /// </summary>
        public int AddConstant(Value val)
        {
            Constants.Add(val);
            return Constants.Count - 1;
        }

        public int GetLineNumber(int instructionOffset)
        {
            int start = 0;
            int end = _lineStarts.Count - 1;

            while (true)
            {
                int mid = start + (end - start) / 2;
                LineStart line = _lineStarts[mid];
                if (instructionOffset < line.Offset)
                {
                    end = mid - 1;
                }
                else if (mid == _lineStarts.Count - 1 ||
                    instructionOffset < _lineStarts[mid + 1].Offset)
                {
                    return line.LineNumber;
                }
                else
                {
                    start = mid + 1;
                }
            }
        }
    }
}
