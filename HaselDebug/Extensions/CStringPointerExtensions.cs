namespace HaselDebug.Extensions;

public static unsafe class CStringPointerExtensions
{
    public static bool IsValid(this CStringPointer ptr)
        => ((nint)ptr.Value).IsValid();
}
