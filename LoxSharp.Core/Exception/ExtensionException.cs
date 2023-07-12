using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public class ExtensionException :Exception
    {
        public ExtensionException(string message)
            :base($"extension error : {message}")
        {

        }
    }
}
