//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JitJotData
{
    using System;
    using System.Collections.Generic;
    
    public partial class CodeUnitAutoId
    {
        public CodeUnitAutoId()
        {
            this.CodeUnits = new HashSet<CodeUnit>();
        }
    
        public long Id { get; set; }
    
        public virtual ICollection<CodeUnit> CodeUnits { get; set; }
    }
}
