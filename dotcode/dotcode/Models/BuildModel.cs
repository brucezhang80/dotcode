using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JitJotData;
using Mirrors;
using dotcode.CompilerService;
using dotcode.lib.common;
using CompilerOutput = dotcode.lib.common.Compiler.CompilerOutput;
using Language = dotcode.lib.common.Language;

namespace dotcode.Models
{
    public class BuildModel
    {
        public static string TempCompilerDirectory
        {
            get
            {
                return GlobalSettings.TempCompilerDir;
            }
        }

        public static IEnumerable<MethodDefinition> GetReflection(string source, IEnumerable<Guid> references, lib.common.Language language)
        {
            var compiledAssembly = CompileInternal(source, references, language).CompiledAssembly;
            if (compiledAssembly == null || compiledAssembly.Length == 0)
                return new MethodDefinition[0];

            return GetReflection(compiledAssembly);
        }

        public static List<MethodDefinition> GetReflection(byte[] assembly)
        {
            var mirror = new Mirror(assembly);
            var reflection = mirror.GetReflection(BindingFlags.Static | BindingFlags.Public);
            return reflection.ToList();
        }

        public static dotcode.lib.common.Compiler.CompilerOutput CompileInternal(string source, IEnumerable<Guid> references, lib.common.Language language)
        {
            var compilerOutput = CompileSource(source, references, language);
            return compilerOutput;
        }

        public static dotcode.lib.common.Compiler.CompilerOutput CompileSource(string source, IEnumerable<Guid> references, lib.common.Language language)
        {
            var codeUnitDto = new CodeUnitDto { Language = dotcode.lib.common.Language.CSharp5, Source = source ?? "" };

            var compilerInput = new CompilerInput
            {
                References = references ?? Enumerable.Empty<Guid>(),
                CodeUnit = codeUnitDto
            };


            var compilerClient = new CompilerServiceClient();
            var compilerOutput = compilerClient.Compile(compilerInput, TempCompilerDirectory);
            return compilerOutput;
        }

        public static Guid SaveCompilerOutputToDb(dotcodedbEntities dotCodeDb, Guid codeUnitId , dotcode.lib.common.Compiler.CompilerOutput compilerOutput)
        {
            var dbCompilerOutput = dotCodeDb.CompilerOutputs.SingleOrDefault(c => c.CodeUnitId == codeUnitId) ?? new JitJotData.CompilerOutput();
            var dbBinary = dotCodeDb.Binaries.SingleOrDefault(d => d.Id == dbCompilerOutput.DllReferenceId) ?? new Binary();

            dbBinary.RawAssembly = compilerOutput.CompiledAssembly;

            dbCompilerOutput.TimeStamp = DateTime.UtcNow;
            dbCompilerOutput.CodeUnitId = codeUnitId;

            if (dbBinary.Id == Guid.Empty)
            {
                dbBinary.Id = Guid.NewGuid();
                dbCompilerOutput.DllReferenceId = dbBinary.Id;
                dotCodeDb.Binaries.Add(dbBinary);
            }

            if (dbCompilerOutput.Id == Guid.Empty)
            {
                dbCompilerOutput.Id = Guid.NewGuid();
                dotCodeDb.CompilerOutputs.Add(dbCompilerOutput);
            }

            dotCodeDb.SaveChanges();

            return dbBinary.Id;
        }

    }
}