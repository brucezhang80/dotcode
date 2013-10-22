using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text.RegularExpressions;
using Sandbox.NET;

namespace SandboxHost
{
    public class SandboxHelper
    {
        private static AppDomain CreateAppDomain(SandboxConfig config, params StrongName[] fullTrustAssemblies)
        {
            var name = "sb_" + config.Uid;
            var evidence = new Evidence();
            evidence.AddHostEvidence(new Zone(SecurityZone.Internet));

            var permissions = new PermissionSet(PermissionState.None);

            // Need to add a private bin path for system executables.
            /*
             * ./privatebin (sandbox data etc)
             * ./bin (user dlls)
             * ./data (files created by user that can be worked on actively... txt, html, json, etc.)
             */
            // We need to care because a user should not be able to overwrite/modify any system .exe or .dll files
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
            if (config.HasFsReadPermission)
            {
                permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read,
                                                           new[] { config.ApplicationBaseDirectory }));
            }

            if (config.HasFsWritePermission)
            {
                permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Write,
                                                           new[] { config.ApplicationDataDirectory }));    
            }
            
            permissions.AddPermission(new WebPermission(NetworkAccess.Connect, new Regex(config.WebPermissionRegex)));

            Environment.CurrentDirectory = config.ApplicationDataDirectory;

            var setup = new AppDomainSetup
            {
                ApplicationBase = config.ApplicationBaseDirectory,
                PrivateBinPath = config.ApplicationBinDirectory,
                ApplicationName = name,
                DisallowBindingRedirects = true,
                DisallowCodeDownload = true,
                DisallowPublisherPolicy = true
            };


            return AppDomain.CreateDomain(name, null, setup, permissions, fullTrustAssemblies);
        }

        public static AppDomain CreateAppDomain(SandboxConfig sandboxConfig, params string[] trustedLibraries)
        {
            var fullTrustAssemblies =
                    trustedLibraries.Select(lib =>
                        {
                            var file = Path.Combine(@"C:\dotcode\_internal\reference", lib);
                            var asm = Assembly.LoadFile(file);
                            return asm;
                        })
                                    .Select(SandboxHelper.CreateStrongName)
                                    .ToArray();
            var sandbox = CreateAppDomain(sandboxConfig, fullTrustAssemblies);
            return sandbox;
        }

        /// http://blogs.msdn.com/b/shawnfa/archive/2005/08/08/449050.aspx
        /// <summary>
        /// Create a StrongName that matches a specific assembly
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// if <paramref name="assembly"/> is null
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// if <paramref name="assembly"/> does not represent a strongly named assembly
        /// </exception>
        /// <param name="assembly">Assembly to create a StrongName for</param>
        /// <returns>A StrongName that matches the given assembly</returns>
        public static StrongName CreateStrongName(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            AssemblyName assemblyName = assembly.GetName();
            
            // get the public key blob
            byte[] publicKey = assemblyName.GetPublicKey();
            if (publicKey == null || publicKey.Length == 0)
                throw new InvalidOperationException("Assembly is not strongly named");

            var keyBlob = new StrongNamePublicKeyBlob(publicKey);

            // and create the StrongName
            return new StrongName(keyBlob, assemblyName.Name, assemblyName.Version);
        }

    }
}
