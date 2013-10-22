using BandwidthThrottle;
using BandwidthThrottle.Hooks;
using Sandbox.NET;

namespace SandboxHost
{
    public class ResourceMonitor
    {
        public NetThrottle NetThrottler { get; set; }
        public FileThrottle FileThrottler { get; set; }

        public ResourceMonitor(SandboxConfig config)
        {
            var netUploadQuota = new IoQuota(config.NetMaxUploadRate, bandwidthCap: config.NetUploadBandwidthLimit);
            var netDownloadQuota = new IoQuota(config.NetMaxDownloadRate, bandwidthCap: config.NetDownloadBandwidthLimit);

            var fileWriteQuota = new IoQuota(config.FileMaxWriteRate, bandwidthCap: config.FileWriteBandwidthLimit);
            var fileReadQuota = new IoQuota(config.FileMaxReadRate, bandwidthCap: config.FileReadBandwidthLimit);

            NetThrottler = new NetThrottle(netDownloadQuota, netUploadQuota);
            FileThrottler = new FileThrottle(fileReadQuota, fileWriteQuota);
        }

        public void StartThrottle()
        {
            NetThrottler.Enable();
            FileThrottler.Enable();
        }

        public void StopThrottle()
        {
            NetThrottler.Disable();
            FileThrottler.Disable();
        }
    }
}
