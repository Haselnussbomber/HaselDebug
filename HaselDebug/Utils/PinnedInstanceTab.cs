using System.Collections.Immutable;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Tabs;

namespace HaselDebug.Utils;

public class PinnedInstanceTab(DebugRenderer DebugRenderer, nint address, Type type) : IDrawableTab
{
    public nint Address => address;
    public Type Type => type;
    public string Title => type.Name;
    public string InternalName => Type.FullName!;
    public bool IsEnabled => true;
    public bool IsPinnable => true;
    public bool CanPopOut => true;
    public bool DrawInChild => true;
    public ImmutableArray<ISubTab<UnlocksTab>>? SubTabs { get; }

    public void Draw()
    {
        DebugRenderer.DrawPointerType(Address, Type, new NodeOptions()
        {
            AddressPath = new AddressPath(Address),
            DefaultOpen = true,
        });
    }
}
