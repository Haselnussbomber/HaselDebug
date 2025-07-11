using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AddonInspectorTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly PinnedInstancesService _pinnedInstances;
    private readonly WindowManager _windowManager;
    private readonly AddonObserver _addonObserver;
    private readonly AtkDebugRenderer _atkDebugRenderer;

    private ushort _selectedAddonId = 0;
    private string _selectedAddonName = string.Empty;
    private bool _sortDirty = true;
    private short _sortColumnIndex = 1;
    private ImGuiSortDirection _sortDirection = ImGuiSortDirection.Ascending;
    private string _addonNameSearchTerm = string.Empty;
    private bool _showPicker;
    private HashSet<Pointer<AtkResNode>> _lastHoveredNodePtrs = [];
    private List<Pointer<AtkResNode>>? _nodePath = null;
    private int _nodePickerSelectionIndex;
    private Vector2 _lastMousePos;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("AddonInspectorTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        DrawAddonList();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        _atkDebugRenderer.DrawAddon(_selectedAddonId, _selectedAddonName, _nodePath);
        if (_nodePath != null)
            _nodePath = null;
        DrawNodePicker();
    }

    private void DrawAddonList()
    {
        using var sidebarchild = ImRaii.Child("AddonListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ObjectUngroup).X - ImGui.GetStyle().ItemSpacing.X);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _addonNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_addonNameSearchTerm);
        var hasSearchTermAutoSelected = false;

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("NodeSelectorToggleButton", FontAwesomeIcon.ObjectUngroup, "Pick Addon/Node", active: _showPicker))
        {
            _showPicker = !_showPicker;
            _nodePickerSelectionIndex = 0;
        }

        using var table = ImRaii.Table("AddonsTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable, new Vector2(-1));
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.PreferSortDescending, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var unitManager = RaptureAtkUnitManager.Instance();
        var allUnitsList = new List<Pointer<AtkUnitBase>>();
        var focusedList = new List<Pointer<AtkUnitBase>>();

        for (var i = 0; i < unitManager->AllLoadedUnitsList.Count; i++)
        {
            var unitBase = unitManager->AllLoadedUnitsList.Entries[i].Value;
            if (unitBase == null)
                continue;

            if (hasSearchTerm && !unitBase->NameString.Contains(_addonNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            allUnitsList.Add(unitBase);
        }

        for (var i = 0; i < unitManager->FocusedUnitsList.Count; i++)
        {
            var unitBase = unitManager->FocusedUnitsList.Entries[i].Value;
            if (unitBase == null)
                continue;

            if (hasSearchTerm && !unitBase->NameString.Contains(_addonNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            focusedList.Add(unitBase);
        }

        allUnitsList.Sort((a, b) => _sortColumnIndex switch
        {
            0 when _sortDirection == ImGuiSortDirection.Ascending => a.Value->Id - b.Value->Id,
            0 when _sortDirection == ImGuiSortDirection.Descending => b.Value->Id - a.Value->Id,
            1 when _sortDirection == ImGuiSortDirection.Ascending => a.Value->NameString.CompareTo(b.Value->NameString),
            1 when _sortDirection == ImGuiSortDirection.Descending => b.Value->NameString.CompareTo(a.Value->NameString),
            _ => 0,
        });

        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
            // if ((unitBase->Flags198 & 0b1100_0000) != 0 || unitBase->HostId != 0)
            //     continue;

            var addonId = unitBase->Id;
            var addonName = unitBase->NameString;

            if (hasSearchTermChanged && !hasSearchTermAutoSelected)
            {
                _selectedAddonId = addonId;
                _selectedAddonName = addonName;
                hasSearchTermAutoSelected = true;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(addonId.ToString());

            ImGui.TableNextColumn(); // Name
            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !unitBase->IsVisible))
            using (ImRaii.PushColor(ImGuiCol.Text, Color.Gold.ToUInt(), focusedList.Contains(unitBase)))
            {
                if (ImGui.Selectable(addonName + $"##Addon_{addonId}_{addonName}", addonId == _selectedAddonId && _selectedAddonName == addonName, ImGuiSelectableFlags.SpanAllColumns))
                {
                    _selectedAddonId = addonId;
                    _selectedAddonName = addonName;
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
                using var windowColors = Color.Gold.Push(ImGuiCol.Border)
                                                    .Push(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

                if (ImGui.Begin("AddonHighligher", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs))
                {
                    var drawList = ImGui.GetForegroundDrawList();
                    var textPos = pos + new Vector2(0, -ImGui.GetTextLineHeight());
                    drawList.AddText(textPos + Vector2.One, Color.Black.ToUInt(), addonName);
                    drawList.AddText(textPos, Color.Gold.ToUInt(), addonName);
                    ImGui.End();
                }
            }

            _imGuiContextMenu.Draw($"##Addon_{addonId}_{addonName}_Context", builder =>
            {
                if (!_debugRenderer.AddonTypes.TryGetValue(addonName, out var type))
                    type = typeof(AtkUnitBase);

                var isPinned = _pinnedInstances.Contains(addonName);

                builder.AddCopyName(_textService, addonName);
                builder.AddCopyAddress(_textService, (nint)unitBase);

                builder.AddSeparator();

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == addonName),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new AddonInspectorWindow(_windowManager, _textService, _addonObserver, _atkDebugRenderer)
                        {
                            AddonId = addonId,
                            AddonName = addonName
                        });
                    }
                });
            });
        }

        var sortSpecs = ImGui.TableGetSortSpecs();
        _sortDirty |= sortSpecs.SpecsDirty;

        if (!_sortDirty)
            return;

        _sortColumnIndex = sortSpecs.Specs.ColumnIndex;
        _sortDirection = sortSpecs.Specs.SortDirection;
        sortSpecs.SpecsDirty = _sortDirty = false;
    }

    private void DrawNodePicker()
    {
        if (!_showPicker)
            return;

        var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();
        var allUnitsList = new List<Pointer<AtkUnitBase>>();

        for (var i = 0; i < raptureAtkUnitManager->AllLoadedUnitsList.Count; i++)
        {
            var unitBase = raptureAtkUnitManager->AllLoadedUnitsList.Entries[i].Value;
            if (unitBase == null || !unitBase->IsFullyLoaded() || !unitBase->IsVisible)
                continue;
            allUnitsList.Add(unitBase);
        }

        allUnitsList.Sort((a, b) => (int)(b.Value->DepthLayer - a.Value->DepthLayer));

        var hoveredDepthLayerAddonNodes = new Dictionary<uint, Dictionary<Pointer<AtkUnitBase>, List<Pointer<AtkResNode>>>>();
        var nodeCount = 0;
        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        var currentHoveredNodePtrs = new HashSet<Pointer<AtkResNode>>();

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
            unitBase->GetWindowBounds(bounds);
            var pos = new Vector2(bounds->Pos1.X, bounds->Pos1.Y);
            var size = new Vector2(bounds->Size.X, bounds->Size.Y);

            if (!bounds->ContainsPoint((int)ImGui.GetMousePos().X, (int)ImGui.GetMousePos().Y))
                continue;

            for (var i = 0; i < unitBase->UldManager.NodeListCount; i++)
            {
                var node = unitBase->UldManager.NodeList[i];
                node->GetBounds(bounds);

                if (!bounds->ContainsPoint((int)ImGui.GetMousePos().X, (int)ImGui.GetMousePos().Y))
                    continue;

                currentHoveredNodePtrs.Add(node);

                if (!hoveredDepthLayerAddonNodes.TryGetValue(unitBase->DepthLayer, out var addonNodes))
                    hoveredDepthLayerAddonNodes.Add(unitBase->DepthLayer, addonNodes = []);

                if (!addonNodes.TryGetValue(unitBase, out var nodes))
                    addonNodes.Add(unitBase, [node]);
                else if (!nodes.Contains(node))
                    nodes.Add(node);

                nodeCount++;
            }
        }

        // Only reset selection index if hovered nodes changed
        if (!currentHoveredNodePtrs.SetEquals(_lastHoveredNodePtrs))
        {
            _nodePickerSelectionIndex = 0;
            _lastHoveredNodePtrs = currentHoveredNodePtrs;
        }

        if (nodeCount == 0)
        {
            _showPicker = false;
            _lastHoveredNodePtrs.Clear();
            return;
        }

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

        if (!ImGui.Begin("NodePicker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground))
            return;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var nodeIndex = 0;
        foreach (var (depthLayer, addons) in hoveredDepthLayerAddonNodes)
        {
            ImGui.TextUnformatted($"Depth Layer {depthLayer}:");

            using var indent = ImRaii.PushIndent();
            foreach (var (unitBase, nodes) in addons)
            {
                ImGui.TextUnformatted($"{unitBase.Value->NameString}:");

                using var indent2 = ImRaii.PushIndent();

                for (var i = nodes.Count - 1; i >= 0; i--)
                {
                    var node = nodes[i].Value;
                    node->GetBounds(bounds);

                    if (_nodePickerSelectionIndex == nodeIndex)
                    {
                        using (ImRaii.PushFont(UiBuilder.IconFont))
                            ImGui.TextUnformatted(FontAwesomeIcon.CaretRight.ToIconString());
                        ImGui.SameLine(0, 0);

                        ImGui.GetForegroundDrawList().AddRectFilled(
                            new Vector2(bounds->Pos1.X, bounds->Pos1.Y),
                            new Vector2(bounds->Pos2.X, bounds->Pos2.Y),
                            new Color(1, 1, 0, 0.5f).ToUInt());

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            _selectedAddonId = unitBase.Value->Id;
                            _selectedAddonName = unitBase.Value->NameString;

                            _nodePath ??= [];
                            _nodePath.Clear();
                            var current = node;
                            while (current != null)
                            {
                                _nodePath.Insert(0, current);
                                current = current->ParentNode;
                            }

                            _nodePickerSelectionIndex = 0;
                            _showPicker = false;
                            _lastHoveredNodePtrs.Clear();
                        }
                    }

                    if ((int)node->Type < 1000)
                    {
                        ImGui.TextUnformatted($"[0x{(nint)node:X}] [{node->NodeId}] {node->Type} Node");
                    }
                    else
                    {
                        var compNode = (AtkComponentNode*)node;
                        var componentInfo = compNode->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
                        if (objectInfo == null) continue;
                        ImGui.TextUnformatted($"[0x{(nint)node:X}] [{node->NodeId}] {objectInfo->ComponentType} Component Node");
                    }

                    nodeIndex++;
                }
            }
        }

        _nodePickerSelectionIndex -= (int)ImGui.GetIO().MouseWheel;
        if (_nodePickerSelectionIndex < 0)
            _nodePickerSelectionIndex = nodeCount - 1;
        if (_nodePickerSelectionIndex > nodeCount - 1)
            _nodePickerSelectionIndex = 0;

        ImGui.End();
    }
}
