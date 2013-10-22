using System;

namespace JobObjectNET.PInvoke
{
    [Flags]
    public enum ProcessCreationFlags : uint
    {
        CreateSuspended = 0x00000004
    }

}
