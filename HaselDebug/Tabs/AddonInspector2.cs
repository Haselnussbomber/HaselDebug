using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AddonInspector2Tab(TextureService TextureService) : DebugTab
{
    private ushort SelectedAddonId = 0;
    private string SelectedAddonName = string.Empty;
    private bool SortDirty = true;
    private short SortColumnIndex = 1;
    private ImGuiSortDirection SortDirection = ImGuiSortDirection.Ascending;
    private ImmutableSortedDictionary<string, Type>? AddonTypes;
    private string AddonNameSearchTerm = string.Empty;
    private bool ShowPicker;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        AddonTypes ??= typeof(Addon).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<Addon>() != null)
            .SelectMany(type => type.GetCustomAttribute<Addon>()!.AddonIdentifiers, (type, addonName) => (type, addonName))
            .ToImmutableSortedDictionary(
                tuple => tuple.addonName,
                tuple => tuple.type);

        using var hostchild = ImRaii.Child("AddonInspector2TabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        TextureService.DrawIcon(60073, 24);
        ImGui.SameLine();
        ImGui.TextUnformatted("Work in progress");

        DrawAddonList();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawAddon();
        DrawNodePicker();
    }

    private void DrawAddonList()
    {
        using var sidebarchild = ImRaii.Child("AddonListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ObjectUngroup).X - ImGui.GetStyle().ItemSpacing.X);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##TextSearch", "Search...", ref AddonNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(AddonNameSearchTerm);
        var hasSearchTermAutoSelected = false;

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("NodeSelectorToggleButton", FontAwesomeIcon.ObjectUngroup, "Pick Addon/Node", active: ShowPicker))
            ShowPicker = !ShowPicker;

        using var table = ImRaii.Table("AddonsTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable, new Vector2(-1));
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

        var allUnitsList = new List<Pointer<AtkUnitBase>>();
        var focusedList = new List<Pointer<AtkUnitBase>>();

        for (var i = 0; i < raptureAtkUnitManager->AllLoadedUnitsList.Count; i++)
        {
            var unitBase = raptureAtkUnitManager->AllLoadedUnitsList.Entries[i].Value;
            if (unitBase == null)
                continue;

            if (hasSearchTerm && !unitBase->NameString.Contains(AddonNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                    continue;

            allUnitsList.Add(unitBase);
        }

        for (var i = 0; i < raptureAtkUnitManager->FocusedUnitsList.Count; i++)
        {
            var unitBase = raptureAtkUnitManager->FocusedUnitsList.Entries[i].Value;
            if (unitBase == null)
                continue;

            if (hasSearchTerm && !unitBase->NameString.Contains(AddonNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            focusedList.Add(unitBase);
        }


        allUnitsList.Sort((a, b) => SortColumnIndex switch
        {
            0 when SortDirection == ImGuiSortDirection.Ascending => a.Value->Id - b.Value->Id,
            0 when SortDirection == ImGuiSortDirection.Descending => b.Value->Id - a.Value->Id,
            1 when SortDirection == ImGuiSortDirection.Ascending => a.Value->NameString.CompareTo(b.Value->NameString),
            1 when SortDirection == ImGuiSortDirection.Descending => b.Value->NameString.CompareTo(a.Value->NameString),
            _ => 0,
        });

        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
            var addonId = unitBase->Id;
            var addonName = unitBase->NameString;

            if (hasSearchTermChanged && !hasSearchTermAutoSelected)
            {
                SelectedAddonId = addonId;
                SelectedAddonName = addonName;
                hasSearchTermAutoSelected = true;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(addonId.ToString());

            ImGui.TableNextColumn(); // Name
            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !unitBase->IsVisible))
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Gold, focusedList.Contains(unitBase)))
            {
                if (ImGui.Selectable(addonName + $"##Addon_{addonId}_{addonName}", addonId == SelectedAddonId && SelectedAddonName == addonName, ImGuiSelectableFlags.SpanAllColumns))
                {
                    SelectedAddonId = addonId;
                    SelectedAddonName = addonName;
                }
            }

            if (ImGui.IsItemHovered() && ImGui.IsKeyDown(ImGuiKey.LeftShift))
            {
                unitBase->GetWindowBounds(bounds);
                var pos = new Vector2(bounds->Pos1.X, bounds->Pos1.Y);
                var size = new Vector2(bounds->Size.X, bounds->Size.Y);

                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(size);

                using var windowStyles = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
                using var windowColors = Colors.Gold.Push(ImGuiCol.Border)
                                                    .Push(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

                if (ImGui.Begin("AddonHighligher", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs))
                {
                    var drawList = ImGui.GetForegroundDrawList();
                    var textPos = pos + new Vector2(0, -ImGui.GetTextLineHeight());
                    drawList.AddText(textPos + Vector2.One, Colors.Black, addonName);
                    drawList.AddText(textPos, Colors.Gold, addonName);
                    ImGui.End();
                }
            }

            using var contextMenu = ImRaii.ContextPopupItem($"##Addon_{addonId}_{addonName}_Context");
            if (contextMenu)
            {
                if (!string.IsNullOrEmpty(addonName) && ImGui.MenuItem("Copy name"))
                {
                    ImGui.SetClipboardText(addonName);
                }

                if (ImGui.MenuItem("Copy address"))
                {
                    ImGui.SetClipboardText($"0x{(nint)unitBase:X}");
                }
            }
        }

        var sortSpecs = ImGui.TableGetSortSpecs();
        SortDirty |= sortSpecs.SpecsDirty;

        if (!SortDirty)
            return;

        SortColumnIndex = sortSpecs.Specs.ColumnIndex;
        SortDirection = sortSpecs.Specs.SortDirection;
        sortSpecs.SpecsDirty = SortDirty = false;
    }

    private void DrawAddon()
    {
        if (string.IsNullOrEmpty(SelectedAddonName))
            return;

        using var hostchild = ImRaii.Child("AddonChild", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);

        if (!AddonTypes!.TryGetValue(SelectedAddonName, out var type))
            type = typeof(AtkUnitBase);

        var unitBase = RaptureAtkUnitManager.Instance()->GetAddonById(SelectedAddonId);
        DebugUtils.DrawPointerType(unitBase, type, new NodeOptions());
    }

    private void DrawNodePicker()
    {
        if (!ShowPicker)
            return;
        /*
        ImGui.SetNextWindowPos(_hoveredWindowPos);
        ImGui.SetNextWindowSize(_hoveredWindowSize);

        using var windowStyles = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
        using var windowColors = Colors.Gold.Push(ImGuiCol.Border)
                                            .Push(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

        if (ImGui.Begin("NodePicker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
        {
            var drawList = ImGui.GetForegroundDrawList();
            var textPos = _hoveredWindowPos + new Vector2(0, -ImGui.GetTextLineHeight());
            drawList.AddText(textPos + Vector2.One, Colors.Black, _hoveredWindowName);
            drawList.AddText(textPos, Colors.Gold, _hoveredWindowName);

            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _showPicker = false;

                if (_hoveredWindowName != "" && !Config.LockedWindows.Any(entry => entry.Name == _hoveredWindowName))
                {
                    Config.LockedWindows.Add(new()
                    {
                        Name = _hoveredWindowName
                    });
                    PluginConfig.Save();
                }
            }

            ImGui.End();
        }*/
    }
}
