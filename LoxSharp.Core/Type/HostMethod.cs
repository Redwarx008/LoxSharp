using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public delegate Value HostMethodDelegate(ClassInstance instance, Value[] args);
    internal class HostMethod
    {
        public string Name { get; private set; }    

        public HostMethodDelegate Method { get; private set; }
        public HostMethod(string name, HostMethodDelegate method) 
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
