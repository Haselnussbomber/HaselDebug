using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using HaselCommon.Services;
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
    public Action<NodeOptions, ImGuiContextMenuBuilder>? DrawContextMenu { get; set; }
    public bool DrawSeStringTreeNode { get; set; } = true;
    public bool RenderSeString { get; set; } = true;
    public AddressPath ResolvedInheritedTypeAddresses { get; set; } = new();
    public bool UseSimpleEventHandlerName { get; set; } = false;
    public ClientLanguage Language { get; set; } = Service.Get<LanguageProvider>().ClientLanguage;
    public bool IsIconIdField { get; set; } = false;
    public bool IsTimestampField { get; set; } = false;
    public bool HexOnShift { get; set; } = false;
    public Pointer<AtkUnitBase>? UnitBase { get; set; } = null;
    public nint HighlightAddress { get; set; } = 0;
    public Type? HighlightType { get; set; } = null;

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
        => this with { Title = title };

    public NodeOptions WithSeStringTitle(string title)
        => this with { SeStringTitle = title };

    public NodeOptions WithSeStringTitle(ReadOnlySeString title)
        => this with { SeStringTitle = title };

    public NodeOptions WithSeStringTitle(ReadOnlySeStringSpan title)
        => this with { SeStringTitle = new(title) };

    public NodeOptions WithSeStringTitleIfNull(string title)
        => SeStringTitle == null && Title == null ? this with { SeStringTitle = title } : this;

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
            DrawSeStringTreeNode = false,
            HighlightAddress = 0,
            HighlightType = null,
        };

    public string GetKey(string prefix) => $"###{prefix}{AddressPath}";
}
