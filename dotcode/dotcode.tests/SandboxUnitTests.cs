using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using dotcode.tests.CompilerService;
using dotcode.tests.RemoteSandbox;

namespace dotcode.tests
{
    [TestClass]
    public class SandboxService
    {
        [TestMethod]
        public void RunSandbox()
        {
            var compilerInput = GenerateDefaultCompilerInputFromFile(@"CSTestFiles\ClassAndConsole.cs");
            var compilerClient = new CompilerServiceClient();
            var compilerOutput = compilerClient.Compile(compilerInput);

            var sandboxInput = new SandboxInput();
            sandboxInput.AssemblyBytes = compilerOutput.CompiledAssembly;
            sandboxInput.MainClass = "ClassAndConsole";
            sandboxInput.EntryPoint = "GetString";

            var sandboxClient = new RemoteSandboxClient();
            var sandboxOutput = sandboxClient.Execute(sandboxInput);

            sandboxOutput.ToString();
        }


        private CompilerInput GenerateDefaultCompilerInputFromFile(string path)
        {
            var source = File.ReadAllText(path);
            return new CompilerInput
            {
                CodeUnit = new CodeUnit { Language = new Language() { Id = 1 }, Source = source }
            };
        }
    }
}
