using HaselDebug.Config;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace HaselDebug.Utils;

public static unsafe class MemoryUtils
{
    public static bool IsPointerValidationEnabled()
    {
        return ServiceLocator.TryGetService<PluginConfig>(out var pluginConfig)
            && pluginConfig.EnablePointerValidation;
    }

    public static bool IsPointerValid(nint ptr)
    {
        return IsPointerValid((void*)ptr);
    }

    public static bool IsPointerValid(void* ptr)
    {
        return ptr != null
            && PInvoke.VirtualQuery(ptr, out var mbi, (nuint)sizeof(MEMORY_BASIC_INFORMATION)) != 0
            && mbi.State == VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT;
    }
}
