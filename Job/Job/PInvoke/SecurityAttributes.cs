using System;
using System.Runtime.InteropServices;

namespace JobObjectNET.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityAttributes
    {
        public int length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
}