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
    public abstract class ThrottleBase : IDisposable
    {
        private IEnumerable<LocalHook> _hooks;
        protected IoQuota _inputQuota;
        protected IoQuota _outputQuota;

        public IoQuota GetInputQuota()
        {
            return _inputQuota.Clone();
        }

        public IoQuota GetOutputQuota()
        {
            return _outputQuota.Clone();
        }

        [SecuritySafeCritical]
        protected ThrottleBase(IoQuota inputQuota, IoQuota outputQuota)
        {
            _outputQuota = outputQuota;
            _inputQuota = inputQuota;

            new PermissionSet(PermissionState.Unrestricted).Assert();
            _hooks = new List<LocalHook>();
            CodeAccessPermission.RevertAssert();
        }

        [SecuritySafeCritical]
        public virtual void Enable()
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            var hooks = SetHooks();
            _hooks = hooks;
            CodeAccessPermission.RevertAssert();
        }

        [SecuritySafeCritical]
        public virtual void Disable()
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            foreach (var hook in _hooks)
            {
                hook.Dispose();
            }

            _hooks = new List<LocalHook>();
            CodeAccessPermission.RevertAssert();
        }

        [SecuritySafeCritical]
        protected abstract IEnumerable<LocalHook> SetHooks();

        [SecuritySafeCritical]
        public void Dispose()
        {
            new PermissionSet(PermissionState.Unrestricted).Assert();
            this.Disable();
            CodeAccessPermission.RevertAssert();
        }
    }
}
