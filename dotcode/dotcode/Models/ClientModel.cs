using System;
using System.Collections.Generic;
using Mirrors;
using dotcode.lib.common.Compiler;

namespace dotcode.Models
{
    public class ClientModel
    {
        public Guid Id { get; set; }
        public int LanguageId { get; set; }
        public long AutoId { get; set; }
        public string Source { get; set; }
        public bool IsPublic { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long VersionId { get; set; }
        public List<Guid> References { get; set; }
        public List<MethodDefinition> Reflection { get; set; }
        public int MethodIndex { get; set; }
        public string RuntimeOutput { get; set; }
        public CompilerOutputSummary CompilerOutput { get; set; }
        public bool LoadSavedReferences { get; set; }
        public bool ShowRunUrl { get; set; }

        public ClientModel()
        {
            this.References = new List<Guid>();
            this.Reflection = new List<MethodDefinition>();
        }
    }
}