using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Sandbox.NET;
using Shared;

namespace SandboxService
{
    public class Service1 : IRemoteSandbox
    {
        public SandboxOutput Execute(SandboxConfig sandboxInput)
        {
            var sandbox = new Sandbox.NET.Sandbox(sandboxInput);
            return sandbox.Run();
        }
    }
}
