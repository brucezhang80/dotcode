using System;
using System.Runtime.InteropServices;

namespace JobObjectNET.PInvoke
{
    public static class Win32
    {
        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms682409(v=vs.85).aspx
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hJob);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateJobObject(object a, string lpName);

        [DllImport("kernel32", SetLastError = true)]
        internal unsafe static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS jobObjectInfoClass,
                                                            [In] void* lpJobObjectInfo,
                                                            uint cbJobObjectInfoLength);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern bool IsProcessInJob(IntPtr processHandle, IntPtr jobHandle, out bool result);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool CreateProcess(string lpApplicationName, string lpCommandLine,
                                                    IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
                                                    bool bInheritHandles, ProcessCreationFlags dwCreationFlags,
                                                    IntPtr lpEnvironment,
                                                    string lpCurrentDirectory, ref StartupInfo lpStartupInfo,
                                                    out ProcessInformation lpProcessInformation);
    }
}
