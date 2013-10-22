using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.NET
{
    [Serializable]
    public class SandboxConfig
    {
        private static string BinPath = "Bin";
        private static string DataPath = "Data";
        // Max out the console stdout buffer to 50k.
        public long ConsoleBufferMax = 1024*50;

        public string Uid { get; set; }
        public int? MemoryLimit { get; set; }
        public TimeSpan MaxRunTime { get; set; }
        public TimeSpan MaxCpuTime { get; set; }

        public string WebPermissionRegex { get; set; }
        public long NetMaxUploadRate { get; set; }
        public long NetMaxDownloadRate { get; set; }
        public long NetUploadBandwidthLimit { get; set; }
        public long NetDownloadBandwidthLimit { get; set; }

        public long FileMaxReadRate { get; set; }
        public long FileMaxWriteRate { get; set; }
        public long FileReadBandwidthLimit { get; set; }
        public long FileWriteBandwidthLimit { get; set; }

        public bool HasFsReadPermission { get; set; }
        public bool HasFsWritePermission { get; set; }

        public string ApplicationBaseDirectory { get; set; }
        public string ApplicationBinDirectory { get { return Path.Combine(ApplicationBaseDirectory, BinPath); } }
        public string ApplicationDataDirectory { get { return Path.Combine(ApplicationBaseDirectory, DataPath); } }
        
        public string SandboxGlobalReferenceLocation { get; set; }

        public SandboxedAssembly SandboxedAssembly { get; set; }
        public IEnumerable<SandboxedAssembly> AdditionalReferences { get; set; }

        public SandboxConfig()
        {
            this.FileReadBandwidthLimit = -1;
            this.FileWriteBandwidthLimit = -1;
        }
    }
}
