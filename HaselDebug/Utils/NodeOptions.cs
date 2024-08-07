using Lumina.Text.ReadOnly;

namespace HaselDebug.Utils;

public record struct NodeOptions
{
    public AddressPath? AddressPath = null;
    public ReadOnlySeString? TitleOverride = null;
    public bool Indent = true;
    public bool DefaultOpen = false;
    public Action? OnHovered = null;
    public float TextOffsetX = 0;
    public bool RenderSeString = true;

    public NodeOptions()
    {
        
    }

    public void EnsureAddressInPath(nint address)
    {
        AddressPath ??= new(address);
        if (AddressPath.Value.Count == 0 || AddressPath.Value.Path[^1] != address)
            AddressPath = AddressPath.Value.With(address);
    }
}
