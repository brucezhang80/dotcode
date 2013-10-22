using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BandwidthThrottle
{
    [Serializable]
    /// <summary>
    /// Allows throttling of resources by limited bytes in/out per unit of time
    /// </summary>
    public class IoQuota
    {
        public const long None = -1;
        private static readonly object Lock = new object();
        private Stopwatch _sw = new Stopwatch();

        /// <summary>
        /// Second-based time unit used for throttling.  
        /// For example, a value of 1 would
        /// be calculated as n bytes per 1 second.  
        /// a value of 60 would mean n bytes per 60 seconds (minute).
        /// </summary>
        public long TimeUnitSeconds { get; set; }

        /// <summary>
        /// Maximum number of bytes allowed to be
        /// transferred in a given time unit.
        /// After this limit is exceeded,
        /// requests are returned as failures until
        /// a sufficient time has elapsed.
        /// </summary>
        public long MaxBytesPerTimeUnit { get; set; }

        /// <summary>
        /// The available bandwidth in bytes available 
        /// for use immediately.  This value is dynamic
        /// and changes every time unit up to the maxbytespertimeunit
        /// Ex:  
        /// The user's limits are 5000 bytes / second
        /// The user transfers 2000 bytes so now the remaining
        /// bandwidth is 3000 within 1 second of the previous transfer.
        /// If the user attempts to transfer more that this amount,
        /// within a second, the delay is calculated as:
        ///     (Now - LastTransferTime) / 
        /// </summary>
        private BandwidthLog _bandwidthLog;

        /// <summary>
        /// Total number of bytes transferred that were handled by this quota instance
        /// </summary>
        public long TotalBytesTransferred { get; set; }

        /// <summary>
        /// Max bytes total.  Maximum number of bytes allowed to be transferred
        /// with this quota.  Any further requests are denied.
        /// </summary>
        public long MaxBytesTotal { get; set; }

        // If the quota limit is set to None, there is no quota limit
        public bool IsQuotaLimitExceeded
        {
            get { return MaxBytesTotal != None && TotalBytesTransferred > MaxBytesTotal; }
        }

        public void AssertQuotaLimitNotExceeded()
        {
            if(IsQuotaLimitExceeded)
                throw new QuotaLimitExceededException();
        }

        private IoQuota()
        {}

        public IoQuota(long maxBytesPerTimeUnit, long timeUnitSeconds = 1, long bandwidthCap = IoQuota.None)
        {
            this._sw = new Stopwatch();
            _sw.Start();
            
            this.MaxBytesPerTimeUnit = maxBytesPerTimeUnit;
            this.TimeUnitSeconds = timeUnitSeconds;
            this.MaxBytesTotal = bandwidthCap;
            this._bandwidthLog = new BandwidthLog(TimeSpan.FromSeconds(TimeUnitSeconds));
        }

        public void Throttle(int transferSizeBytes)
        {
            try
            {
                AssertQuotaLimitNotExceeded();

                lock (Lock)
                {
                    // Add the operation to our log, trimming old item out
                    _bandwidthLog.AddOperation(transferSizeBytes, _sw.ElapsedMilliseconds);
                    this.TotalBytesTransferred += transferSizeBytes;

                    var sleepMillisec = CalculateSleepTimeMilliSeconds();
                    Thread.Sleep((int)(sleepMillisec));

                   AssertQuotaLimitNotExceeded();
                }
            }
            catch (Exception ex)
            {
                
                throw new Exception(ex.Message);
            }
        }

        private long CalculateSleepTimeMilliSeconds()
        {
            var sleepTimeMillisec = 0.0F;

            var maxBytesPerTimeUnit = this.MaxBytesPerTimeUnit;
            var ops = _bandwidthLog.GetOperations(_sw.ElapsedMilliseconds);

            var bytesDownloaded = ops.Sum(z => z.Size);
            var bytesPerSecond = bytesDownloaded;
            var maxBytesPerSecond = (float) (maxBytesPerTimeUnit) / TimeUnitSeconds;

            if (bytesPerSecond > maxBytesPerSecond)
                sleepTimeMillisec = (TimeUnitSeconds*1000F)*(bytesPerSecond/maxBytesPerSecond);


            return (long) sleepTimeMillisec;
        }

        internal IoQuota Clone()
        {
            return new IoQuota
            {
                MaxBytesPerTimeUnit = this.MaxBytesPerTimeUnit,
                TotalBytesTransferred = this.TotalBytesTransferred,
                MaxBytesTotal = this.MaxBytesTotal,
                TimeUnitSeconds = this.TimeUnitSeconds
            };
        }
    }
}