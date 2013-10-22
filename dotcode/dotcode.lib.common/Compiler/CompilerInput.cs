using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotcode.lib.common
{
    public class CompilerInput
    {
        public CodeUnitDto CodeUnit { get; set; }
        public IEnumerable<Guid> References { get; set; }
    }
}
