using System;

namespace SharpES.Core
{
    public class ForeignRuntimeException : Exception
    {
        public ForeignRuntimeException(string message)
            : base(message)
        {

        }
    }
}
