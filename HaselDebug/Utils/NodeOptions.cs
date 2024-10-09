using Dalamud.Game;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Utils;

public record struct NodeOptions
{
    public NodeOptions() { }

    public AddressPath AddressPath { get; set; } = new();
    public string? Title { get; set; } = null;
    public Color? TitleColor { get; set; } = null;
    public ReadOnlySeString? SeStringTitle { get; set; } = null;
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

    public NodeOptions WithSeStringTitle(string title)
        => this with { SeStringTitle = title.ToReadOnlySeString() };

    public NodeOptions WithSeStringTitle(ReadOnlySeString title)
        => this with { SeStringTitle = title };

    public NodeOptions WithSeStringTitleIfNull(string title)
        => SeStringTitle == null && Title == null ? this with { SeStringTitle = title.ToReadOnlySeString() } : this;

    public NodeOptions WithSeStringTitleIfNull(ReadOnlySeString title)
        => SeStringTitle == null && Title == null ? this with { SeStringTitle = title } : this;

    public NodeOptions ConsumeTreeNodeOptions()
        => this with
        {
            SeStringTitle = null,
            Title = null,
            TitleColor = null,
            DefaultOpen = false,
            DrawContextMenu = null,
            OnHovered = null,
            DrawSeStringTreeNode = false
        };

    public string GetKey(string prefix) => $"###{prefix}{AddressPath}";

    internal NodeOptions WithSeStringTitle(object value)
    {
        throw new NotImplementedException();
    }
}
