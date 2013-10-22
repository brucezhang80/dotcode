using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using BandwidthThrottle;
using BandwidthThrottle.Hooks;
using HttpWebClient;

namespace Program
{
    public partial class Test : EasyHook.IEntryPoint
    {
        public static void Main()
        {
            for (int quotaLimit = 15; quotaLimit < 150; quotaLimit += 20)
            {
                Console.WriteLine("Quota Limit = " + 30);
                for (int i = 0; i < 3; i++)
                {
                    var downloadLimit = new IoQuota(1024 * 5);
                    var uploadLimit = downloadLimit;// new IoQuota(1024 * 5, 1, IoQuota.None);

                    using (var netThrottle = new NetThrottle(uploadLimit, downloadLimit))
                    {
                        netThrottle.Enable();
                        DoNetwork();
                        //DoFileWork();
                    }
                    Console.Read();
                }
            }
                
            Console.ReadLine();
        }

        private static void DoNetwork()
        {
            Stopwatch s = new Stopwatch();
            s.Start();

            var httpWebClient = new HttpWebClient.HttpWebClient();
            var task = httpWebClient.HttpGet("http://www.reddit.com");
            task.Wait();
            var str = task.Result.GetResponseStringAndDispose();

            //var str = new WebClient().DownloadString("http://www.google.com");
            //str += new WebClient().DownloadString("http://www.google.com");
            //File.WriteAllText("test.html", str);
            //var y = File.ReadAllText("test.html");

            //task.Wait(10000);
            //task.Start();
            //task.Wait();
            //dtask.Wait();

            //File.WriteAllText("test.html", task.Result.GetResponseStringAndDispose());
            
            s.Stop();
            
            var totalKb = (str.Length / 1024.0);
            var seconds = s.Elapsed.TotalSeconds;
            Console.WriteLine("kb: {0}, sec: {1}, kb/s: {2}", totalKb, seconds, totalKb / seconds);
        }
        
        private static void DoFileWork()
        {

            var data = new string(Enumerable.Repeat('x', 1024 * 1000).ToArray());
            File.WriteAllText("test.txt", data);

            Stopwatch s = new Stopwatch();
            s.Start();

            var str = File.ReadAllText("test.txt");

            s.Stop();

            var totalKb = (str.Length / 1024.0);
            var seconds = s.Elapsed.TotalSeconds;
            Console.WriteLine("kb: {0}, sec: {1}, kb/s: {2}", totalKb, seconds, totalKb / seconds);
        }


        #region Hooks


        private static IoQuota _netQuota;
        private static StringBuilder sb = new StringBuilder(65536);

        #endregion



        public Test()
        {
            try
            {
                
            }

            catch (Exception ExtInfo)
            {
                Console.WriteLine("Error creating the Hook: \r\n{0}", ExtInfo.Message);
            }
        }


    }
}
