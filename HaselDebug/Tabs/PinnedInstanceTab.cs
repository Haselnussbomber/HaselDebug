using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

public class PinnedInstanceTab(DebugRenderer DebugRenderer, nint address, Type type) : IDrawableTab
{
    public nint Address => address;
    public Type Type => type;
    public string Title => type.Name;
    public string InternalName => Type.FullName!;
    public bool DrawInChild => true;

    public void Draw()
    {
        DebugRenderer.DrawPointerType(Address, Type, new NodeOptions()
        {
            AddressPath = new AddressPath(Address),
            DefaultOpen = true,
        });
    }
}
