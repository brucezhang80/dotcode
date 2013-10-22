using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BandwidthThrottle
{
    public static class W32
    {
        internal struct Kernel32
        {
            [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern bool ReadFileEx(IntPtr hFile,
                                                 [Out] byte[] lpBuffer,
                                                 uint nNumberOfBytesToRead,
                                                 [In] ref NativeOverlapped lpOverlapped,
                                                 IntPtr lpCompletionRoutine);

            [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern int ReadFile(
                    IntPtr hFile,
                    IntPtr lpBuffer,
                    uint nNumberOfBytesToRead,
                    out uint lpNumberOfBytesRead,
                    IntPtr lpOverlapped
                );

            [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern int WriteFile(
                    IntPtr hFile,
                    IntPtr lpBuffer,
                    uint nNumberOfBytesToWrite,
                    out uint lpNumberOfBytesWritten,
                    IntPtr lpOverlapped
                );

            [DllImport("kernel32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern int WriteFileEx(
                    IntPtr hFile,
                    IntPtr lpBuffer,
                    uint nNumberOfBytesToWrite,
                    out uint lpNumberOfBytesWritten,
                    IntPtr lpOverlapped
                );
        }

        internal struct Winsock2
        {
            [DllImport("Ws2_32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern int recvfrom(
                [In] int socket,
                [Out] IntPtr buf,
                [In] int len,
                [In] int flags,
                [Out] IntPtr from,
                [In][Out] IntPtr fromlen
                );

            [DllImport("Ws2_32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern int recv(
                IntPtr socketHandle,
                IntPtr buf,
                int count,
                int socketFlags
                );

            [DllImport("Ws2_32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern int send(
                IntPtr s,
                IntPtr buf,
                int len,
                int flags
                );

            [DllImport("ws2_32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            internal static extern SocketError WSARecv([In] IntPtr socketHandle, [In, Out] WSABuffer[] buffers, [In] int bufferCount, IntPtr bytesTransferred, [In, Out] ref SocketFlags socketFlags, [In] IntPtr overlapped, [In] IntPtr completionRoutine);

            [StructLayout(LayoutKind.Sequential)]
            internal struct WSABuffer
            {
                internal int Length;
                internal IntPtr Pointer;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct sockaddr
            {
                internal int sa_family;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
                internal String sa_data;
            }
        }
    }
}
