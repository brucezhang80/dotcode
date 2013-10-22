using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BandwidthThrottle
{
    [Serializable]
    public class QuotaLimitExceededException : Exception
    {
        public QuotaLimitExceededException(){}
    }
}
