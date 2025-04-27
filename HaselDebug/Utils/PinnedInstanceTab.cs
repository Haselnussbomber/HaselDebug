using System.Collections.Immutable;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Utils;

public class PinnedInstanceTab(DebugRenderer debugRenderer, nint address, Type type) : IDebugTab
{
    public nint Address => address;
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
        debugRenderer.DrawPointerType(Address, Type, new NodeOptions()
        {
            AddressPath = new AddressPath(Address),
            DefaultOpen = true,
        });
    }
}
