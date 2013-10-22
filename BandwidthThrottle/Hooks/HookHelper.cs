using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using EasyHook;

namespace BandwidthThrottle.Hooks
{
    public static class HookHelper
    {
        [SecuritySafeCritical]
        public static LocalHook SetLocalHook(string lib, string func, Delegate hook)
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            var localHook = LocalHook.Create(
                    LocalHook.GetProcAddress(lib, func),
                    hook,
                    null);

            localHook.ThreadACL.SetExclusiveACL(new[] { -1 });
            SecurityPermission.RevertAssert();
            return localHook;
        }
    }
}
