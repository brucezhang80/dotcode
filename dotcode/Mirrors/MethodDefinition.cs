using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirrors
{
    public class MethodDefinition : MarshalByRefObject
    {
        public string MethodName;
        public ParameterType[] Parameters;

        public MethodDefinition()
        {
            Parameters = new ParameterType[0];
        }
    }
}
