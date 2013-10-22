using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using JitJotData;
using Sandbox.NET;
using Shared;
using WebMatrix.WebData;
using dotcode.Extensions;
using dotcode.Models;
using dotcode.lib.common.Compiler;

namespace dotcode.Controllers
{
    public class SandboxController : ApiController
    {
        [System.Web.Http.HttpGet]
        public HttpResponseMessage Run(long codeunitid, long version, string method, string jsonArgs, string retattr = null)
        {
            var dotcodedb = new dotcodedbEntities();
            var log = new RunLog();
            var ip = GetClientIp(Request);
            string output = null;

            log.IPAddress = ip;
            log.Input = method + ", " + jsonArgs;
            log.UserId = WebSecurity.CurrentUserId;
            log.CodeUnitId =
                dotcodedb.CodeUnits
                .Where(c => c.AutoId == codeunitid && c.VersionId == version)
                .Select(c => c.Id)
                .SingleOrDefault();

            SandboxOutput sandboxOutput = null;
            try
            {
                sandboxOutput = SandboxModel.Execute(Request.Headers.Host, codeunitid, version, method, jsonArgs);
                output = sandboxOutput.ToJsonString();
            }

            finally
            {
                log.Output = output;
                dotcodedb.RunLogs.Add(log);
                dotcodedb.SaveChanges();
            }

            return sandboxOutput.ToJsonHttpResponse(retattr);
        }

        private string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop;
                prop = (RemoteEndpointMessageProperty)this.Request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else
            {
                return null;
            }
        }
    }
}
