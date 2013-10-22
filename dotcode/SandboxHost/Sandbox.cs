using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Web;
using dotcode.lib.common.Sandbox;

namespace SandboxHost
{
    public class Sandbox
    {
        private const string _sandboxPath = @"C:\dotcode_sandbox";
        private readonly string _uniqueId;

        public Sandbox()
        {
            _uniqueId = Guid.NewGuid().ToString();
        }

        public SandboxOutput ExecuteAssembly(SandboxInput sandboxIn)
        {
            var mainClass = sandboxIn.MainClass;
            var entryMethod = sandboxIn.EntryPoint;

            var assemblyName = CopyAssemblyToSandbox(sandboxIn.AssemblyBytes, _uniqueId);
            var appdomain = CreateAppDomain(_uniqueId);

            var x = appdomain.ExecuteAssemblyByName(assemblyName);
//            var assembly = appdomain.Load(assemblyName);
            
            //var sandboxWorkingPath = GetSandboxWorkingDir(uniqueId);


/*            Type type = assembly.GetType(mainClass);
            var obj = assembly.CreateInstance(entryMethod);

            string retVal = type.InvokeMember(entryMethod,
                              BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                              null,
                              obj,
                              null) as String ?? String.Empty;
            */
            return new SandboxOutput{ExecutionTimeMillisec = 0, PeakMemoryUsage = 0, Stdout = "."};
        }

        private AssemblyName CopyAssemblyToSandbox(byte[] assemblyBytes, string uniqueId)
        {
            var asm = Assembly.Load(assemblyBytes);
            
            var fileName = asm.ManifestModule.ScopeName;
            string sandboxDir = String.Format("{0}", _sandboxPath);
            var binFileName = fileName; //"bin_" + uniqueId + ".dll";
            var filepath = Path.Combine(sandboxDir, binFileName);
            var dataDir = GetSandboxWorkingDir(uniqueId);

            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            File.WriteAllBytes(filepath, assemblyBytes);

            return asm.GetName();
        }

        private static string GetSandboxWorkingDir(string uniqueid)
        {
            var path = Path.Combine(_sandboxPath, "data_" + uniqueid);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private AppDomain CreateAppDomain(string uniqueId)
        {
            var sandboxDir = _sandboxPath;

            if (!Directory.Exists(sandboxDir)) Directory.CreateDirectory(sandboxDir);

            var appDomainBaseDir = sandboxDir;

            var setup = new AppDomainSetup { ApplicationBase = appDomainBaseDir, PrivateBinPath = appDomainBaseDir, ShadowCopyFiles = "true"};
            var evidence = new Evidence(AppDomain.CurrentDomain.Evidence);
            var permissionSet = new PermissionSet(PermissionState.Unrestricted);
            var fp = new FileIOPermission(PermissionState.Unrestricted);

            permissionSet.AddPermission(fp);
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.AllFlags));
            permissionSet.AddPermission(new UIPermission(PermissionState.Unrestricted));

            AppDomain sandbox = AppDomain.CreateDomain("sbox_" + uniqueId, evidence, setup);
            return sandbox;
        }
    }
}