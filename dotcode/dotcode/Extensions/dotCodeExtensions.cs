using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dotcode.Extensions
{
    public static class DotCodeExtensions
    {
        public static string ToJsonString(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented) ?? "{ }";
        }

        public static HttpResponseMessage ToJsonHttpResponse(this object obj, string retattr = null)
        {
            var response = new HttpResponseMessage();

            if (!String.IsNullOrWhiteSpace(retattr))
            {
                var sandboxJObject = JObject.FromObject(obj);
                var token = sandboxJObject[retattr];

                if (token != null)
                {
                    var tokenVal = token.Value<string>();
                    response.Content = new StringContent(tokenVal ?? "");
                }
            }

            else
            {
                response.Content = new StringContent(JsonConvert.SerializeObject(obj, Formatting.Indented) ?? "");
            }

            response.Content.Headers.Clear();
            response.Content.Headers.Add("content-type", "text/html");

            return response;
        }
    }
}