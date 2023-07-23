using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public delegate Value ForeignMethodDelegate(ClassInstance instance, Value[] args);
    internal class ForeignMethod
    {
        public string Name { get; private set; }    

        public ForeignMethodDelegate Method { get; private set; }
        public ForeignMethod(string name, ForeignMethodDelegate method) 
        { 
            Name = name;    
            Method = method;
        }

        public override string ToString() 
        {
            return $"<Name>";    
        }
    }
}
