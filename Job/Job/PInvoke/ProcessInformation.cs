using System;
using System.Runtime.InteropServices;

namespace JobObjectNET.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }
}