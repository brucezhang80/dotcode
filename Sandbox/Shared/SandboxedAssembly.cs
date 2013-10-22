using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.NET
{
    [Serializable]
    public class SandboxedAssembly
    {
        public string EntryPointClass { get; set; }
        public string EntryPointMethod { get; set; }
        public BindingFlags BindingFlags { get; set; }
        public byte[] AssemblyBytes { get; set; }
        public string AssemblyName { get; set; }
        public string JsonArgs { get; set; }
    }
}
