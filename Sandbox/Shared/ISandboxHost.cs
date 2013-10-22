using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Sandbox.NET;

namespace Shared
{
    [ServiceContract()]
    public interface ISandboxHost
    {
        [OperationContract]
        SandboxOutput Execute(SandboxConfig sandboxConfig);

        [OperationContract]
        void Die();
    }
}
