using Dalamud.Game;
using HaselCommon.Extensions;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Utils;

public record struct NodeOptions
{
    public NodeOptions() { }

    public AddressPath AddressPath { get; set; } = new();
    public ReadOnlySeString? Title { get; set; } = null;
    public bool Indent { get; set; } = true;
    public bool DefaultOpen { get; set; } = false;
    public Action? OnHovered { get; set; } = null;
    public Action<NodeOptions>? DrawContextMenu { get; set; }
    public bool DrawSeStringTreeNode { get; set; } = true;
    public bool RenderSeString { get; set; } = true;
    public ClientLanguage Language { get; set; } = ClientLanguage.English;

    public ImGuiTreeNodeFlags GetTreeNodeFlags(ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.SpanAvailWidth)
    {
        if (DefaultOpen)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;

        return flags;
    }

    public NodeOptions WithAddress(nint address)
        => this with { AddressPath = AddressPath.With(address) };

    public NodeOptions WithAddress(nint[] addresses)
        => this with { AddressPath = AddressPath.With(addresses) };

    public NodeOptions WithTitle(string title)
        => this with { Title = title.ToReadOnlySeString() };

    public NodeOptions WithTitle(ReadOnlySeString title)
        => this with { Title = title };

    public NodeOptions WithTitleIfNull(string title)
        => Title == null ? this with { Title = title.ToReadOnlySeString() } : this;

    public NodeOptions WithTitleIfNull(ReadOnlySeString title)
        => Title == null ? this with { Title = title } : this;

    public string GetKey(string prefix) => $"###{prefix}{AddressPath}";
}
