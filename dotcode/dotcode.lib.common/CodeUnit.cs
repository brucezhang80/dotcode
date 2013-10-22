using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotcode.lib.common
{
    public class CodeUnitDto
    {
        public Language Language { get; set; }
        public DateTime LastEdit { get; set; }
        public string Source { get; set; }
    }
}
