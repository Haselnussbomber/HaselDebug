using Dalamud.Game;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Utils;

public record NodeOptions
{
    public AddressPath AddressPath = new();
    public ReadOnlySeString? TitleOverride = null;
    public bool Indent = true;
    public bool DefaultOpen = false;
    public Action? OnHovered = null;
    public Action<NodeOptions>? DrawContextMenu;
    public float TextOffsetX = 0;
    public bool RenderSeString = true;
    public ClientLanguage Language = ClientLanguage.English;

    public void EnsureAddressInPath(nint address)
    {
        if (AddressPath.Path.Length == 0 || AddressPath.Path[^1] != address)
            AddressPath = AddressPath.With(address);
    }
}
