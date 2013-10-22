using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace Mirrors
{
    public class Mirror : MarshalByRefObject, IDisposable
    {
        private bool _isLoaded;
        private AppDomain _appDomain;
        private readonly Glass _loader;

        private Mirror()
        {
            _isLoaded = false;
        }

        public Mirror(byte[] assemblyBytes) : this()
        {
            Reset();

            var type = typeof(Glass);
            var handle = Activator.CreateInstanceFrom(_appDomain, type.Assembly.ManifestModule.FullyQualifiedName, type.FullName);
            _loader = (Glass) handle.Unwrap();
            _loader.SetCurrentAssembly(assemblyBytes);
        }

        public void Reset()
        {
            _appDomain = CreateAppDomain(Environment.CurrentDirectory);
            _isLoaded = true;
        }

        public FileVersionInfo GetAssemblyFileVersionInfo(byte[] assemblyBytes)
        {
            var tmpfile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tmpfile, assemblyBytes);
                return FileVersionInfo.GetVersionInfo(tmpfile);
            }

            finally
            {
                File.Delete(tmpfile);
            }
        }

        public string GetImageRuntimeVersion()
        {
            return _loader.InternalGetRuntimeVersion();
        }

        /// <summary>
        /// Loads an assembly passed in as a byte[] into a separate appdomain, and reflects on the dll
        /// </summary>
        /// <returns>Returns a list of public static methods in addition to their parameter lists, if any.</returns>
        public MethodDefinition[] GetReflection(BindingFlags bindingFlags, Func<MethodInfo, bool> selector = null)
        {
            if (selector == null)
            {
                selector = m => m.DeclaringType != null && !m.IsSpecialName && !m.ContainsGenericParameters;
            }

            var reflection = _loader.GetReflectionInternal(BindingFlags.Static | BindingFlags.Public, selector);

            return reflection.Select(r => new MethodDefinition
            {
                MethodName = r.MethodName,
                Parameters = r.Parameters.Select(p => new ParameterType
                {
                    ParameterName = p.ParameterName,
                    TypeName = p.TypeName
                }).ToArray()
            }).DistinctBy(m => m.MethodName).ToArray();
        }

        private static AppDomain CreateAppDomain(string dir)
        {
            var name = "r_" + Guid.NewGuid().ToString().Replace("-", "");
            var permissions = new PermissionSet(PermissionState.None);
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

            var setup = new AppDomainSetup
            {
                ApplicationBase = dir,
                ApplicationName = name,
                DisallowBindingRedirects = true,
                DisallowCodeDownload = true,
                DisallowPublisherPolicy = true
            };

            return AppDomain.CreateDomain(name, null, setup, permissions);
        }

        public void Dispose()
        {
            ValidateLoaded();

            if (_appDomain == null || !_isLoaded) return;

            AppDomain.Unload(_appDomain);
            _isLoaded = false;
        }

        private void ValidateLoaded()
        {
            if (!_isLoaded)
            {
                throw new ObjectDisposedException("Mirror");
            }
        }
    }
}
