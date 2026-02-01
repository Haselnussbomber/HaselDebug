using System.Collections.Immutable;
using System.Reflection;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Utils;

public unsafe class PinnedInstanceTab(DebugRenderer debugRenderer, Type type) : IDebugTab
{
    public Type Type => type;
    public string Title => type.Name;
    public string InternalName => Type.FullName!;
    public bool IsEnabled => true;
    public bool IsPinnable => true;
    public bool CanPopOut => true;
    public bool DrawInChild => true;
    public ImmutableArray<IDebugTab>? SubTabs { get; }

    public void Draw()
    {
        var instanceMethod = type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public);
        if (instanceMethod == null)
            return;

        var ptr = (Pointer?)instanceMethod.Invoke(null, null);
        if (ptr == null)
            return;

        var address = (nint)Pointer.Unbox(ptr);

        debugRenderer.DrawPointerType(address, Type, new NodeOptions()
        {
            AddressPath = new AddressPath(address),
            DefaultOpen = true,
        });
    }
}
