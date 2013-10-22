using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using JobObjectNET.PInvoke;

namespace ManagedJob
{
    public class Job : IDisposable
    {
        private readonly IntPtr _jobObjectHandle;
        private bool _disposed;

        public Job(
            bool killJobOnClose = true, 
            ulong maxProcessMemory = ulong.MaxValue,
            long maxProcessUserTimeMillisec = long.MaxValue)
        {
            // Create job object and get handler
            IntPtr jobObjectHandle = Win32.CreateJobObject(IntPtr.Zero, null);
            if (jobObjectHandle == IntPtr.Zero)
                throw new Win32Exception();
            _jobObjectHandle = jobObjectHandle;

            this.SetJobObjectLimits(killJobOnClose, maxProcessMemory, maxProcessUserTimeMillisec);
        }

        public void AddProcessToJob(Process process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            AssignProcess(process.Handle);
        }

        private void CloseHandleCheckingResult(IntPtr handle)
        {
            bool result = Win32.CloseHandle(handle);

            if (!result)
                throw new Win32Exception();
        }

        public void AssignCurrentProcess()
        {
            var phandle = Process.GetCurrentProcess().Handle;
            AssignProcess(phandle);
        }

        private void AssignProcess(IntPtr handle)
        {
            if (IsProcessInJobCheckingResult(handle, _jobObjectHandle))
                return;

            if (IsProcessInJobCheckingResult(handle, IntPtr.Zero))
                throw new InvalidOperationException(
                    "Requested process already belongs to another job group. Check http://stackoverflow.com/a/4232259/3205 for help.");

            bool result = Win32.AssignProcessToJobObject(_jobObjectHandle, handle);
            if (!result)
                throw new Win32Exception();
        }

        private bool IsProcessInJobCheckingResult(IntPtr processHandle, IntPtr jobObjectHandle)
        {
            bool status;
            bool result = Win32.IsProcessInJob(processHandle, jobObjectHandle, out status);
            if (!result)
                throw new Win32Exception();
            return status;
        }

        public Process StartProcessInJob(ProcessStartInfo processStartInfo)
        {
            var startInfo = new StartupInfo();
            ProcessInformation processInfo;

            string workingDirectory = string.IsNullOrEmpty(processStartInfo.WorkingDirectory)
                                            ? null
                                            : processStartInfo.WorkingDirectory;

            if (String.IsNullOrWhiteSpace(workingDirectory))
            {
                var dir = Path.GetFileNameWithoutExtension(processStartInfo.FileName);

                if(!String.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);
                
                workingDirectory = dir;
            }

            bool result = Win32.CreateProcess(processStartInfo.FileName, ' ' + processStartInfo.Arguments, IntPtr.Zero,
                                        IntPtr.Zero,
                                        false, ProcessCreationFlags.CreateSuspended, IntPtr.Zero,
                                        workingDirectory, ref startInfo, out processInfo);

            if (!result)
                throw new Win32Exception();

            AssignProcess(processInfo.hProcess);

            uint status = Win32.ResumeThread(processInfo.hThread);
            if (status != 0 && status != 1)
                throw new Win32Exception();

            CloseHandleCheckingResult(processInfo.hProcess);
            CloseHandleCheckingResult(processInfo.hThread);

            return Process.GetProcessById((int) processInfo.dwProcessId);
        }

        private unsafe void SetJobObjectLimits(bool killOnJobClose, ulong maxProcessMemory, long userTimeMilliseconds)
        {
            var basicLimitFlags = new LimitFlags();
            var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            limits.BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION();
                
            if (killOnJobClose) basicLimitFlags |= LimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            if (maxProcessMemory != ulong.MaxValue)
            {
                basicLimitFlags |= LimitFlags.JOB_OBJECT_LIMIT_PROCESS_MEMORY;
                limits.ProcessMemoryLimit = new UIntPtr(maxProcessMemory);
                limits.BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION();
            }

            if (userTimeMilliseconds != long.MaxValue)
            {
                basicLimitFlags |= LimitFlags.JOB_OBJECT_LIMIT_PROCESS_TIME;
                var nanoSeconds100 = userTimeMilliseconds*10;
                limits.BasicLimitInformation.PerProcessUserTimeLimit = nanoSeconds100; 
            }

            limits.BasicLimitInformation.LimitFlags = basicLimitFlags;

            bool result = SetInformationJobObject<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(
                JOBOBJECTINFOCLASS.ExtendedLimitInformation, 
                &limits);

            if (!result)
                throw new Win32Exception();
        }

        private unsafe bool SetInformationJobObject<T>(JOBOBJECTINFOCLASS jObjectInfoClass, void* limit)
        {
            return Win32.SetInformationJobObject(
                _jobObjectHandle, jObjectInfoClass,
                limit,
                (uint) Marshal.SizeOf(typeof(T)));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            CloseHandleCheckingResult(_jobObjectHandle);
        }
    }
}
