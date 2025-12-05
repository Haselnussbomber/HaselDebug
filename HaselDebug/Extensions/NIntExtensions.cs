using Windows.Win32;
using Windows.Win32.System.Memory;

namespace HaselDebug.Extensions;

public static unsafe class NIntExtensions
{
    public static bool IsValid(this nint address)
    {
        return address != 0
            && PInvoke.VirtualQuery((void*)address, out var mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) != 0
            && mbi.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT;
    }
}
