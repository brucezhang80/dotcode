//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace dotCodeDataSource
{
    using System;
    using System.Collections.Generic;
    
    public partial class DllReference
    {
        public DllReference()
        {
            this.CompilerOutputs = new HashSet<CompilerOutput>();
        }
    
        public System.Guid Id { get; set; }
        public string FileName { get; set; }
        public byte[] RawAssembly { get; set; }
    
        public virtual ICollection<CompilerOutput> CompilerOutputs { get; set; }
    }
}
