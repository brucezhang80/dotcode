using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace dotcode.lib.common.Compiler
{
    [DataContract]
    public class CompilerError
    {
        [DataMember]
        public bool IsWarning { get; set; }

        [DataMember]
        public string ErrorCode { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int Line { get; set; }

        [DataMember]
        public int Column { get; set; }
    }
}
