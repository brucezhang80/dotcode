using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JitJotData;
using Sandbox.NET;
using Shared;
using WebMatrix.WebData;

namespace dotcode.Models
{
    public class SandboxModel
    {
        public static SandboxOutput Execute(string hostName, long codeunitid, long version, string method, string jsonArgs)
        {
            var dotcodedb = new dotcodedbEntities();
            var sandboxOutput = new SandboxOutput();

            if (codeunitid >= 0 && !string.IsNullOrWhiteSpace(method))
            {
                var codeUnitId = dotcodedb.CodeUnits.Where(c => c.AutoId == codeunitid && c.VersionId == version).Select(c => c.Id).SingleOrDefault();
                var compilerOutId = dotcodedb.CompilerOutputs.Where(c => c.CodeUnitId == codeUnitId).Select(c => c.DllReferenceId).SingleOrDefault();
                var dbBinary = dotcodedb.Binaries.SingleOrDefault(d => d.Id == compilerOutId);
                var references = ReferenceModel.GetProjectReferences(codeUnitId);

                if (dbBinary != null)
                    sandboxOutput = RunInternal(hostName, dbBinary.RawAssembly, references, method, jsonArgs);
            }

            return sandboxOutput;
        }

        internal static SandboxOutput RunInternal(string hostName, byte[] assemblyBytes, IEnumerable<Guid> references, string method, string jsonArgs, Guid? id = null)
        {
            if (WebSecurity.IsAuthenticated) WebSecurity.Logout();
            if (!Regex.IsMatch(hostName, @"^(localhost(:\d+)?)|(run.jitjot.net)$"))
                return null;

            var tokens = method.Split('.');
            if (tokens.Count() < 3)
                return new SandboxOutput() { Stderr = "Invalid method name.  Format: Namespace.Class.Method" };

            var className = String.Join(".", tokens.Take(tokens.Length - 1));
            var methodName = tokens.Last();

            var sbConfig = GetSbConfig(assemblyBytes, references, className, methodName, jsonArgs);
            var sandbox = new Sandbox.NET.Sandbox(sbConfig);
            var sandboxOutput = sandbox.Run();

            return sandboxOutput;
        }

        private static IEnumerable<SandboxedAssembly> GetSandboxAssemblies(IEnumerable<Guid> referenceIds)
        {
            var dotCodeDb = new dotcodedbEntities();
            var binaries = referenceIds
                .Select(r => dotCodeDb.Binaries.SingleOrDefault(b => b.Id == r))
                .Where(r => r != null)
                .Select(r => new SandboxedAssembly
                        {
                            AssemblyBytes = r.RawAssembly,
                            AssemblyName = r.FileName,
                        });

            return binaries;
        }

        private static SandboxConfig GetSbConfig(byte[] assemblyBytes, IEnumerable<Guid> referenceIds, string className, string methodName, string jsonArgs)
        {
            var sandboxbasedir = @"C:\dotCode";
            var uid = Guid.NewGuid().ToString();

            var sandboxedAssembly = new SandboxedAssembly
            {
                AssemblyBytes = assemblyBytes,
                BindingFlags = BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
                EntryPointClass = className,
                EntryPointMethod = methodName,
                AssemblyName = Guid.NewGuid() + ".dll",// compilerOutput.FileName,
                JsonArgs = jsonArgs
            };

            var sandboxConfig = new SandboxConfig()
            {
                Uid = uid,
                MemoryLimit = 1024 * 1024 * 25,
                MaxCpuTime = TimeSpan.FromMilliseconds(1000 * 20),
                MaxRunTime = TimeSpan.FromMilliseconds(1000 * 20),
                SandboxGlobalReferenceLocation = Path.Combine(sandboxbasedir, "_internal\reference"),
                ApplicationBaseDirectory = Path.Combine(sandboxbasedir, uid),
                SandboxedAssembly = sandboxedAssembly,
                NetDownloadBandwidthLimit = (long)(1024 * 1024 * 1.5),
                NetUploadBandwidthLimit = (long)(1024 * 1024 * .25),
                NetMaxDownloadRate = 1024 * 50,
                NetMaxUploadRate = 1024 * 10,
                WebPermissionRegex = String.Format("^https?://.+"),
                FileMaxReadRate = 1024 * 1024 * 5,
                FileReadBandwidthLimit = 1024 * 1024 * 20,
                FileMaxWriteRate = 1024 * 50,
                FileWriteBandwidthLimit = 1024 * 1024,
                AdditionalReferences = GetSandboxAssemblies(referenceIds),
                HasFsReadPermission = true,
                HasFsWritePermission = false
            };

            return sandboxConfig;
        }


    }
}
