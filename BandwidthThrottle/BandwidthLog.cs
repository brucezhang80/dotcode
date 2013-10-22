using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BandwidthThrottle
{
    public class BandwidthLog
    {
        private List<IoOperation> _ioOperations; 
        private readonly long _logHistorySpan;

        public BandwidthLog(TimeSpan logHistorySpan)
        {
            _logHistorySpan = (long) logHistorySpan.TotalMilliseconds;
            _ioOperations = new List<IoOperation>(128);
        }

        public void AddOperation(int bytesTransferred, long currentElapsedMilliseconds)
        {
            // Add operation, remove old data
            TruncateHistory(currentElapsedMilliseconds);

            var ioOperation = new IoOperation { Size = bytesTransferred, TimeStampMilliseconds = currentElapsedMilliseconds };
            _ioOperations.Add(ioOperation);
        }

        public IoOperation[] GetOperations(long currentElapsedMs)
        {
            TruncateHistory(currentElapsedMs);
            return _ioOperations.ToArray();
        }

        /// <summary>
        /// Removes old items from the history log and recalculates fields.
        /// </summary>
        private void TruncateHistory(long currentElapsedMilliseconds)
        {
            var oldOps = _ioOperations.Where(x => (currentElapsedMilliseconds - x.TimeStampMilliseconds) > _logHistorySpan).ToArray();
            foreach (var ioOperation in oldOps)
            {
                _ioOperations.Remove(ioOperation);
            }            
        }

        public void PurgeHistory()
        {
            _ioOperations = new List<IoOperation>();
        }
    }

    public struct IoOperation
    {
        public long Size;
        public long TimeStampMilliseconds;
    }
}
