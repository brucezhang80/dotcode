using System;
using System.IO;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using dotcode.tests.CompilerService;

namespace dotcode.tests
{
    [TestClass]
    public class CompilerUnitTests
    {
        [TestMethod]
        public void TestNullInput()
        {
            var client = new CompilerServiceClient();

            try
            {
                var compilerInput = GenerateDefaultCompilerInputFromFile(@"CSTestFiles\EmptyFile.cs");
                var compilerOutput = client.Compile(compilerInput);
                Assert.Fail();
            }
            catch (FaultException<ArgumentNullException> ex)
            {
                // Source should be null.
            }
        }

        [TestMethod]
        public void TestCompiler()
        {
            var client = new CompilerServiceClient();

            var compilerInput = GenerateDefaultCompilerInputFromFile(@"CSTestFiles\ClassAndConsole.cs");
            var compilerOutput = client.Compile(compilerInput);
            Assert.IsTrue(compilerOutput.CompilerErrors.Length == 0);
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
