using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mirrors
{
    [Serializable]
    internal class Glass
    {
        private Assembly _assembly;

        internal void SetCurrentAssembly(byte[] assemblyBytes)
        {
            _assembly = Assembly.Load(assemblyBytes);
        }

        internal string InternalGetRuntimeVersion()
        {
            return _assembly.ImageRuntimeVersion;
        }

        internal IEnumerable<MethodDefinition> GetReflectionInternal(BindingFlags bindingFlags, Func<MethodInfo, bool> selector)
        {
            return
                (from m in _assembly.GetTypes().SelectMany(t => t.GetMethods(bindingFlags))
                 let declaringType = m.DeclaringType
                 where selector(m)
                 let methodName = declaringType.FullName + "." + m.Name
                 select new MethodDefinition
                 {
                     MethodName = methodName,
                     Parameters = m.GetParameters().Select(p => new ParameterType
                     {
                         TypeName = p.ParameterType.Name,
                         ParameterName = p.Name
                     }).ToArray()
                 }).ToArray();
        }

    }
}
