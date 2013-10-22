using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using EasyHook;

namespace BandwidthThrottle.Hooks
{
    public partial class NetThrottle : ThrottleBase, IDisposable
    {
        public NetThrottle(IoQuota downloadQuota, IoQuota uploadQuota) : base(downloadQuota, uploadQuota)
        {
            
        }

        [SecuritySafeCritical]
        protected override IEnumerable<LocalHook> SetHooks()
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            NativeAPI.LoadLibrary("ws2_32.dll");

            var hooks = new List<LocalHook>
                {
                    HookHelper.SetLocalHook("ws2_32.dll", "recv", new NetThrottle.RecvDelegate(recv_Hooked)),
                    HookHelper.SetLocalHook("ws2_32.dll", "send", new NetThrottle.SendDelegate(send_Hooked)),
                    HookHelper.SetLocalHook("ws2_32.dll", "WSARecv", new NetThrottle.WSARecvDelegate(WSARecv_Hooked)),
                    HookHelper.SetLocalHook("ws2_32.dll", "recvfrom", new NetThrottle.RecvFromDelegate(recvFrom_Hooked))
                };

            CodeAccessPermission.RevertAssert();
            return hooks;
        }
    }

    [SecuritySafeCritical]
    public partial class NetThrottle
    {
        SocketError WSARecv_Hooked([In] IntPtr socketHandle, [In, Out] W32.Winsock2.WSABuffer[] buffers, [In] int bufferCount, IntPtr bytesTransferred, [In, Out] ref SocketFlags socketFlags, [In] IntPtr overlapped, [In] IntPtr completionRoutine)
        {
            try
            {
                _inputQuota.AssertQuotaLimitNotExceeded();
                var x = W32.Winsock2.WSARecv(socketHandle, buffers, bufferCount, bytesTransferred, ref socketFlags,
                                             overlapped,
                                             completionRoutine);

                var bytesCount = Marshal.ReadInt32(bytesTransferred);
                _inputQuota.Throttle(bytesCount);
                
                return x;
            }

            catch (QuotaLimitExceededException)
            {
                return SocketError.ConnectionAborted;
            }

            catch (Exception ex)
            {
                Console.WriteLine("wsarecv" + ex.Message);
                return SocketError.ConnectionAborted;
            }
        }

        int recvFrom_Hooked(int socket, IntPtr buf, int len, int flags, IntPtr from, IntPtr fromlen)
        {
            try
            {   
                _inputQuota.AssertQuotaLimitNotExceeded();
                int bytesCount = W32.Winsock2.recvfrom(socket, buf, len, flags, from, fromlen);
                _inputQuota.Throttle(bytesCount);
                
                return bytesCount;
            }
            catch (QuotaLimitExceededException)
            {
                Console.WriteLine("quota limit exceeded. " + _inputQuota.MaxBytesTotal);
                return -1;
            }

            catch (Exception ex)
            {
                Console.WriteLine("recvfrom" + ex.Message);
                return -1;
            }
        }

        int recv_Hooked(IntPtr socketHandle, IntPtr buf, int count, int socketFlags)
        {
            try
            {
                // We check beforehand, because we cannot actually
                // throttle packet transfer speed, but we can throttle
                // the rate that the function returns data.

                _inputQuota.AssertQuotaLimitNotExceeded();
                int bytesCount = W32.Winsock2.recv(socketHandle, buf, count, socketFlags);
                _inputQuota.Throttle(bytesCount);

                return bytesCount;
            }
            catch (QuotaLimitExceededException)
            {
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("recv" + ex.Message);
                return -1;
            }
        }

        int send_Hooked(IntPtr socketHandle, IntPtr buf, int count, int socketFlags)
        {
            try
            {
                _outputQuota.AssertQuotaLimitNotExceeded();
                int bytesCount = W32.Winsock2.send(socketHandle, buf, count, socketFlags);
                _outputQuota.Throttle(bytesCount);

                return bytesCount;
            }
            catch (QuotaLimitExceededException)
            {
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("send" + ex.Message);
                return -1;
            }
        }
    }

    // Delegate definitions
    public partial class NetThrottle
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int RecvDelegate(
            IntPtr socketHandle,
            IntPtr buf,
            int count,
            int socketFlags
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int RecvFromDelegate(
            int socket,
            IntPtr buf,
            int len,
            int flags,
            IntPtr from,
            IntPtr fromlen
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int SendDelegate(
            IntPtr socketHandle,
            IntPtr buf,
            int count,
            int socketFlags
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate SocketError WSARecvDelegate(
            [In] IntPtr socketHandle,
            [In, Out] W32.Winsock2.WSABuffer[] buffers,
            [In] int bufferCount,
            IntPtr bytesTransferred,
            [In, Out] ref SocketFlags socketFlags,
            [In] IntPtr overlapped,
            [In] IntPtr completionRoutine);
    }
}
