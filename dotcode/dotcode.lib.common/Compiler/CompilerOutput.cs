using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace dotcode.lib.common.Compiler
{
    [DataContract]
    public abstract class CompilerOutputBase
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public bool HasWarnings { get; set; }
        [DataMember]
        public bool HasErrors { get; set; }
        [DataMember]
        public CompilerError[] CompilerErrors { get; set; }
        [DataMember]
        public DateTime TimeStamp { get; set; }
    }

    [DataContract]
    public class CompilerOutput : CompilerOutputBase
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public byte[] CompiledAssembly { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    [DataContract]
    public class CompilerOutputSummary : CompilerOutputBase
    {
        [DataMember]
        public long CodeUnitId { get; set; }

        public CompilerOutputSummary() { }

        public CompilerOutputSummary(CompilerOutput compilerOutput)
        {
            CompilerErrors = compilerOutput.CompilerErrors;
            HasErrors = compilerOutput.HasErrors;
            TimeStamp = compilerOutput.Timestamp;
        }
    }
}
