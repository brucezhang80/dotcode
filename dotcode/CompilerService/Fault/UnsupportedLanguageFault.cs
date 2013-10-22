using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using dotcode.lib.common;

namespace CompilerService.Fault
{
    [DataContract]
    public class UnsupportedLanguageFaultContract
    {
        [DataMember]
        public Language Language { get; set; }
    }
}