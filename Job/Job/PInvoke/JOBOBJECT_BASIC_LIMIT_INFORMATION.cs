using System;
using System.Runtime.InteropServices;

namespace JobObjectNET.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public Int64 PerProcessUserTimeLimit;
        public Int64 PerJobUserTimeLimit;
        public LimitFlags LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public Int16 ActiveProcessLimit;
        public Int64 Affinity;
        public Int16 PriorityClass;
        public Int16 SchedulingClass;
    }
}