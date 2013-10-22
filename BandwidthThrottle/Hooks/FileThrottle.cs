using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyHook;

namespace BandwidthThrottle.Hooks
{
    [SecuritySafeCritical]
    public partial class FileThrottle : ThrottleBase
    {

        public FileThrottle(IoQuota readQuota, IoQuota writeQuota) : base(readQuota, writeQuota)
        {
        }

        [SecuritySafeCritical]
        protected override IEnumerable<LocalHook> SetHooks()
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            NativeAPI.LoadLibrary("kernel32.dll");

            var hooks = new List<LocalHook>
                {
                    HookHelper.SetLocalHook("kernel32.dll", "ReadFile", new ReadFileDelegate(ReadFile_Hooked)),
                    HookHelper.SetLocalHook("kernel32.dll", "ReadFileEx", new ReadFileExDelegate(ReadFileEx_Hooked)),
                    HookHelper.SetLocalHook("kernel32.dll", "WriteFile", new WriteFileDelegate(WriteFile_Hooked)),
                    HookHelper.SetLocalHook("kernel32.dll", "WriteFileEx", new WriteFileExDelegate(WriteFileEx_Hooked))
                };

            CodeAccessPermission.RevertAssert();
            return hooks;
        }
    }

    public partial class FileThrottle
    {
        int ReadFile_Hooked(IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped)
        {

            var retVal = W32.Kernel32.ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, lpOverlapped);

            _inputQuota.Throttle((int)lpNumberOfBytesRead);
            if (_inputQuota.IsQuotaLimitExceeded) return -1;

            return retVal;
        }

        private bool ReadFileEx_Hooked(IntPtr hfile, byte[] lpbuffer, uint nnumberofbytestoread, ref NativeOverlapped lpoverlapped, IntPtr lpcompletionroutine)
        {
            var retval = W32.Kernel32.ReadFileEx(hfile, lpbuffer, nnumberofbytestoread, ref lpoverlapped, lpcompletionroutine);
            
            
            _inputQuota.Throttle((int)nnumberofbytestoread);
            if (_inputQuota.IsQuotaLimitExceeded) return false;

            return retval;
        }

        private int WriteFileEx_Hooked(IntPtr hFile,
                    IntPtr lpBuffer,
                    uint nNumberOfBytesToWrite,
                    out uint lpNumberOfBytesWritten,
                    IntPtr lpOverlapped)
        {
            var retval = W32.Kernel32.WriteFileEx(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten,
                                            lpOverlapped);

            _inputQuota.Throttle((int)lpNumberOfBytesWritten);
            if (_inputQuota.IsQuotaLimitExceeded) return -1;

            return retval;
        }

        private int WriteFile_Hooked(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped)
        {
            var retval = W32.Kernel32.WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, lpOverlapped);

            _inputQuota.Throttle((int)lpNumberOfBytesWritten);
            if (_inputQuota.IsQuotaLimitExceeded) return -1;

            return retval;
        }
    }

    // Delegate definitions
    public partial class FileThrottle
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate int ReadFileDelegate(
            IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate bool ReadFileExDelegate(
            IntPtr hFile,
            [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            [In] ref NativeOverlapped lpOverlapped,
            IntPtr lpCompletionRoutine
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate int WriteFileDelegate(IntPtr hFile,
                                               IntPtr lpBuffer,
                                               uint nNumberOfBytesToWrite,
                                               out uint lpNumberOfBytesWritten,
                                               IntPtr lpOverlapped);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate int WriteFileExDelegate(IntPtr hFile,
                                               IntPtr lpBuffer,
                                               uint nNumberOfBytesToWrite,
                                               out uint lpNumberOfBytesWritten,
                                               IntPtr lpOverlapped);
    }
}
