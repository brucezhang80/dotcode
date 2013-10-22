using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ManagedJob;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var jo = new Job(true, 1024 * 1024 * 3, 5000);
            
            var psi = new ProcessStartInfo("TargetApplication.exe");
            jo.AssignCurrentProcess();
            /*var process = jo.StartProcessInJob(psi);

            process.ErrorDataReceived += (sender, eventArgs) => Console.WriteLine("ERR: " + eventArgs.Data);
            process.Exited += (sender, eventArgs) => Console.WriteLine("EXIT: {0}; PPT: {1}", process.ExitCode, process.PrivilegedProcessorTime);
            */
            var error = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            string errorMsg = error.Message;
            
            Console.WriteLine(error.NativeErrorCode +  ": " + errorMsg);
            Console.ReadLine();
        }
    }
}
