using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Sandbox.NET
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SandboxClientService : ISandboxClient
    {
        public SandboxOutput SandboxOutput { get; set; }
        private SandboxConfig _config;

        public SandboxClientService()
        {
            
        }

        public SandboxClientService(SandboxConfig config, Process process)
        {
            _config = config;
            _config.AdditionalReferences = config.AdditionalReferences.Select(s => new SandboxedAssembly(){AssemblyName = s.AssemblyName});
            SandboxOutput = new SandboxOutput();
        }

        public SandboxConfig GetSandboxConfig()
        {
            return _config;
        }

        public void NotifyReady(Uri sandboxHostUri)
        {
            var binding = new BasicHttpBinding {MaxReceivedMessageSize = Int32.MaxValue};
            using (var channelFactory = new ChannelFactory<ISandboxHost>(binding))
            {
                ISandboxHost wcf = null;
                try
                {
                    wcf = channelFactory.CreateChannel(new EndpointAddress(sandboxHostUri));
                    if (wcf != null)
                    {
                        var x = wcf.Execute(_config);
                        SandboxOutput = x;
                    }
                }

                catch (Exception ex)
                {
                    SandboxOutput.Stderr = ex.Message;
                }

                finally
                {
                    try
                    {
                        if (wcf != null)
                            wcf.Die();
                    }
                    catch
                    {
                        
                    }
                }
            }
        }
    }
}
