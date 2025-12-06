using HaselDebug.Config;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace HaselDebug.Utils;

public static unsafe class MemoryUtils
{
    public static bool IsPointerValid(nint ptr)
    {
        return IsPointerValid((void*)ptr);
    }

    public static bool IsPointerValid(void* ptr)
    {
        if (ptr == null || !ServiceLocator.TryGetService<PluginConfig>(out var pluginConfig))
            return false;

        if (!pluginConfig.EnablePointerValidation)
            return true;

        return PInvoke.VirtualQuery(ptr, out var mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) != 0
            && mbi.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT;
    }
}
