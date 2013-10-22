using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Sandbox.NET;

namespace Shared
{
    [ServiceContract]
    public interface ISandboxClient
    {
        [OperationContract]
        SandboxConfig GetSandboxConfig();

        [OperationContract]
        void NotifyReady(Uri uri);
    }
}
