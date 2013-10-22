using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace dotcode.lib.common
{
    [DataContract]
    public class BinaryReference
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public byte[] Hash { get; set; }

        [DataMember]
        public Guid BinaryId { get; set; }

        [DataMember]
        public DateTime CreateDate { get; set; }
    }
}
