using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using CompilerService.Fault;
using JitJotData;
using dotcode.lib.common;
using CompilerOutput = dotcode.lib.common.Compiler.CompilerOutput;
using Language = dotcode.lib.common.Language;

namespace CompilerService
{
    public class CompilerService : ICompilerService
    {
        private List<string> TempFiles = new List<string>(); 

        public CompilerOutput Compile(CompilerInput compilerInput, string tempCompilerDir)
        {
            try
            {
                VerifyCompilerInput(compilerInput);

                var codeDomProvider = GetCodeDomProvider(compilerInput.CodeUnit.Language);
                var compilerParameters = GetCompilerParameters(compilerInput, tempCompilerDir);

                var compilerResults = codeDomProvider.CompileAssemblyFromSource(compilerParameters, compilerInput.CodeUnit.Source);
                var compilerOutput = GetCompilerOutput(compilerResults);

                return compilerOutput;

            }

            finally
            {
                if (TempFiles != null)
                {
                    foreach (var file in TempFiles)
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        private CompilerOutput GetCompilerOutput(CompilerResults compilerResults)
        {
            var assemblyPath = compilerResults.PathToAssembly;
            var compilerErrors = compilerResults.Errors.Cast<System.CodeDom.Compiler.CompilerError>().ToArray();

            var compilerOutput = new CompilerOutput
                {
                    Timestamp = DateTime.UtcNow, 
                    FileName = String.Empty,
                    CompiledAssembly = new byte[0]
                };

            var hasErrors = compilerErrors.Any(e => !e.IsWarning);
            var hasWarnings = compilerErrors.Any(e => e.IsWarning);

            compilerOutput.HasErrors = hasErrors;
            compilerOutput.HasWarnings = hasWarnings;

            if (!hasErrors && !String.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
            {
                var assemblyBytes = File.ReadAllBytes(assemblyPath);
                compilerOutput.CompiledAssembly = assemblyBytes;
                compilerOutput.FileName = assemblyPath;

                File.Delete(assemblyPath);   
            }

            compilerOutput.CompilerErrors = compilerErrors
                .Select(c => new dotcode.lib.common.Compiler.CompilerError
                    {
                        Column = c.Column, 
                        ErrorCode = c.ErrorNumber,
                        ErrorMessage = c.ErrorText,
                        IsWarning = c.IsWarning,
                        Line = c.Line
                    })
                .ToArray();

            return compilerOutput;
        }

        private CodeDomProvider GetCodeDomProvider(Language language)
        {
            if (language.Id == Language.CSharp5.Id)
            {
                var providerOptions = new Dictionary<string, string>() { {"CompilerVersion", "v4.0"} };
                return CodeDomProvider.CreateProvider("CSharp", providerOptions);
            }

            throw UnsupportedLanguageFault(language);
        }

        private CompilerParameters GetCompilerParameters(CompilerInput compilerInput, string tempCompilerDir)
        {
            var compilerParams = new CompilerParameters();

            if (compilerInput.CodeUnit.Language.Id == Language.CSharp5.Id)
            {
                Directory.CreateDirectory(tempCompilerDir);
                var tempFileName = Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".dll";
                var outputPath = Path.Combine(tempCompilerDir, tempFileName);

                compilerParams.GenerateInMemory = false;
                compilerParams.GenerateExecutable = false;
                compilerParams.OutputAssembly =  outputPath;
                compilerParams.TreatWarningsAsErrors = false;
                compilerParams.WarningLevel = 4;
                compilerParams.ReferencedAssemblies.Add("System.Core.dll");
                compilerParams.ReferencedAssemblies.Add("System.dll");
                compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
                AddReferencedBinaries(compilerInput, compilerParams);
            }

            else
            {
                throw UnsupportedLanguageFault(compilerInput.CodeUnit.Language);
            }

            return compilerParams;
        }

        private void AddReferencedBinaries(CompilerInput compilerInput, CompilerParameters compilerParams)
        {
            var dotCodeDb = new dotcodedbEntities();
            var binIds = compilerInput.References;
            var binaries = dotCodeDb.Binaries.Where(bin => binIds.Contains(bin.Id));

            foreach (var binary in binaries)
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "dc_" + Guid.NewGuid() + @"\");
                Directory.CreateDirectory(tempDir);
                var path = Path.Combine(tempDir, binary.FileName);
                File.WriteAllBytes(path, binary.RawAssembly);
                compilerParams.ReferencedAssemblies.Add(path);
                TempFiles.Add(path);
            }
        }

        private FaultException<UnsupportedLanguageFaultContract> UnsupportedLanguageFault(Language language)
        {
            return new FaultException<UnsupportedLanguageFaultContract>(
                    new UnsupportedLanguageFaultContract() { Language = language });
        }

        private void VerifyCompilerInput(CompilerInput compilerInput)
        {
            if (compilerInput == null) throw new FaultException<ArgumentNullException>(new ArgumentNullException("CompilerInput"));
            if (compilerInput.CodeUnit == null) throw new FaultException<ArgumentNullException>(new ArgumentNullException("CompilerInput.CodeUnit"));
            if (compilerInput.CodeUnit.Language == null) throw new FaultException<ArgumentNullException>(new ArgumentNullException("Language"));

            if (String.IsNullOrEmpty(compilerInput.CodeUnit.Source.Trim())) throw new FaultException<ArgumentNullException>(new ArgumentNullException("Source"));
        }
    }
}
