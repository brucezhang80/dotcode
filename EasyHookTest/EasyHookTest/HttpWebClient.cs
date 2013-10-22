using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpWebClient
{
    /// <summary>
    /// Class to make it easier for developers to make muliple requests to a site while preserving
    /// state such as cookies and headers.
    /// </summary>
    public class HttpWebClient
    {
        public WebProxy WebProxy { get; set; }
        public CookieContainer CookieContainer { get; set; }

        private readonly int _timeoutMilliseconds;

        static HttpWebClient()
        {
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.MaxServicePoints = 500;
            ServicePointManager.DefaultConnectionLimit = 500;
            ServicePointManager.Expect100Continue = false;
        }

        // Default to 30 second timeout
        public HttpWebClient(WebProxy proxy = null, int timeoutMilliseconds = 30000)
        {
            this.WebProxy = proxy;
            this.CookieContainer = new CookieContainer();

            _timeoutMilliseconds = timeoutMilliseconds;
        }

        public async Task<HttpWebResponse> HttpGet(string url)
        {
            var request = ConstructHttpGetRequest(url);
            return await GetHttpResponse(request);
        }

        public async Task<HttpWebResponse> HttpPost(string url, string postData)
        {
            var request = ConstructHttpPostRequest(url, postData);
            return await GetHttpResponse(request);
        }

        public async Task<HttpWebResponse> HttpPost(string url, Dictionary<string, string> valueDictionary)
        {
            var postData = GeneratePostBody(valueDictionary);
            var request = ConstructHttpPostRequest(url, postData);
            return await GetHttpResponse(request);
        }

        public HttpWebRequest ConstructHttpGetRequest(string url)
        {
            return CreateDefaultHttpWebRequest(url, "GET");
        }

        public HttpWebRequest ConstructHttpPostRequest(string url, Dictionary<string, string> valueDictionary,
                                                       string host = null)
        {
            var postData = GeneratePostBody(valueDictionary);
            return ConstructHttpPostRequest(url, postData);
        }

        public HttpWebRequest ConstructHttpPostRequest(string url, string postData)
        {
            var request = CreateDefaultHttpWebRequest(url, "POST");
            WriteToHttpWebRequestStream(request, postData);
            return request;
        }

        protected void WriteToHttpWebRequestStream(HttpWebRequest httpWebRequest, string data)
        {
            WriteToHttpWebRequestStream(httpWebRequest, Encoding.ASCII.GetBytes(data));
        }

        protected void WriteToHttpWebRequestStream(HttpWebRequest httpWebRequest, byte[] data)
        {
            using (var requestStream = httpWebRequest.GetRequestStream())
            {
                var contentBytes = data;
                requestStream.Write(contentBytes, 0, contentBytes.Length);
            }
        }

        protected HttpWebRequest CreateDefaultHttpWebRequest(string url, string method, string accept = null)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            // Default to HTTP 1.0
            request.ProtocolVersion = HttpVersion.Version10;

            request.Timeout = _timeoutMilliseconds;
            request.Host = new Uri(url).Host;
            request.CookieContainer = CookieContainer;
            request.Method = method;
            request.Accept =
                "application/json,text/javascript,text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers["Accept-Charset"] = "ISO-8859-1,utf-8;q=0.7,*;q=0.7";

            if (this.WebProxy != null)
                request.Proxy = this.WebProxy;

            return request;
        }

        public async Task<HttpWebResponse> GetHttpResponse(HttpWebRequest request)
        {
            HttpWebResponse response =
                await
                Task<HttpWebResponse>.Factory.FromAsync(request.BeginGetResponse,
                                                        r => (HttpWebResponse) request.EndGetResponse(r), null);
            return response;
        }

        public void ClearSession()
        {
            if (CookieContainer != null)
                CookieContainer = new CookieContainer();
        }

        public static string GeneratePostBody(Dictionary<string, string> postValues)
        {
            var values = String.Join("&", postValues.Select(kv => String.Join("=", kv.Key, kv.Value)));
            return values;
        }


    }

    public static class Extensions
    {
        public static string GetResponseStringAndDispose(this WebResponse httpWebResponse)
        {
            // Cache webresponses and their respective body properties.
            //  Need to create a way to clear the dictionary when the reponse is no longer needed
            //   to conserve resources.

            string responseString = "";
            using (httpWebResponse)
            {
                using (var responseStream = httpWebResponse.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var sr = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            // Allocate 200kb by default.  Most requests will never exceed this so we should not incur a performance hit due to constant reallocations
                            var sb = new StringBuilder(1024*200);

                            while (!sr.EndOfStream)
                            {
                                sb.Append((char) sr.Read());
                            }

                            responseString = sb.ToString();
                        }
                    }
                }
            }

            return responseString;
        }
    }
}