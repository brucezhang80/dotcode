using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using Sandbox.NET;

namespace Shared
{
    public class ServiceHelper
    {
        public static Uri GetServiceUri(string uniqueId)
        {
            var port = 8322;
            var id = uniqueId;
            var url = String.Format("http://localhost:{0}/{1}", port, id);

            return new Uri(url);            
        }

        public static ServiceHost StartService<T>(Uri serviceAddress, T serviceSingleton)
        {
            if(serviceSingleton == null) 
                throw new ArgumentNullException("serviceSingleton");

            var host = new ServiceHost(serviceSingleton, serviceAddress);


            var smb = new ServiceMetadataBehavior
            {
                HttpGetEnabled = true,
                MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            
            };

            host.Description.Behaviors.Add(smb);
            
            host.Open();
            return host;
        }
    }
}
