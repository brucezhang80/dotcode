using System;
using System.Diagnostics;
using System.Threading;
using System.ServiceModel;
using Shared;

namespace SandboxHost
{
    public class Host
    {
        // Uses WCF self hosting to communicate with hosting sandbox API.
        // Pass arguments to method as object and typeof()
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { };
            Uri callbackUri = null;
            try
            {
                callbackUri = new Uri(args[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid or null URI parameter");
                return;
            }

            Init(callbackUri);
            Thread.Sleep(int.MaxValue);
        }

        private static void Init(Uri uri)
        {
            // Create client to original service
            // Get the config
            // Start the hosted service
            // Call client and say ready.

            var process = Process.GetCurrentProcess();
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (sender, args) => Console.WriteLine("--- stderr --- " + Environment.NewLine + (args.Data == null ? "" : (args.Data.Trim() + Environment.NewLine)) + "---");

            var channelFactory = new ChannelFactory<ISandboxClient>(new BasicHttpBinding());
            var sandboxClient = channelFactory.CreateChannel(new EndpointAddress(uri));
            
            var sandboxConfig = sandboxClient.GetSandboxConfig();
            if (sandboxConfig == null) return;

            var sandboxHostService = new SandboxHostService(sandboxConfig);
            var hostUri = ServiceHelper.GetServiceUri(sandboxConfig.Uid);

            ServiceHelper.StartService(hostUri, sandboxHostService);

            sandboxClient.NotifyReady(hostUri);
        }
    }
}
