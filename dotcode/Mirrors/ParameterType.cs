using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirrors
{
    public class ParameterType : MarshalByRefObject
    {
        public string TypeName { get; set; }
        public string ParameterName { get; set; }
        public string Value { get; set; }
    }
}
