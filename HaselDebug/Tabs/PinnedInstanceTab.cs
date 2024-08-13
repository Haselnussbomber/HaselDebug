using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

public class PinnedInstanceTab(DebugRenderer DebugRenderer, nint address, Type type) : IDrawableTab
{
    public nint Address { get; } = address;
    public Type Type { get; init; } = type;
    public string Title { get; init; } = type.Name;
    public string InternalName => Type.FullName!;
    public bool DrawInChild => true;

    public void Draw()
    {
        DebugRenderer.DrawPointerType(Address, Type, new NodeOptions() {
            AddressPath = new AddressPath(Address),
            DefaultOpen = true,
        });
    }
}
