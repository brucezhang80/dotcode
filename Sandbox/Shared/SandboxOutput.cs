using System;
using System.Runtime.Serialization;

namespace Shared
{
    [Serializable]
    [DataContract]
    public class SandboxOutput
    {
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public string Stdout { get; set; }
        [DataMember]
        public string Stderr { get; set; }

        [DataMember]
        public string ReturnType { get; set; }

        [DataMember]
        public string RetVal { get; set; }

        [DataMember]
        public long PeakMemoryUsage { get; set; }
        [DataMember]
        public long RuntimeMilliseconds { get; set; }

        [DataMember]
        public long UploadSpeedAvg { get; set; }
        [DataMember]
        public long DownloadSpeedAvg { get; set; }
        [DataMember]
        public long BytesDownloaded { get; set; }
        [DataMember]
        public long BytesUploaded { get; set; }

        [DataMember]
        public long WriteSpeedAvg { get; set; }
        [DataMember]
        public long ReadSpeedAvg { get; set; }
        [DataMember]
        public long BytesWritten { get; set; }
        [DataMember]
        public long BytesRead { get; set; }
    }
}
