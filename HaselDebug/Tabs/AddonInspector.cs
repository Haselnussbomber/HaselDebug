using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Windows;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AddonInspectorTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly TypeService _typeService;
    private readonly DebugRenderer _debugRenderer;
    private readonly PinnedInstancesService _pinnedInstances;
    private readonly WindowManager _windowManager;
    private readonly AddonObserver _addonObserver;
    private readonly AtkDebugRenderer _atkDebugRenderer;
    private readonly NavigationService _navigationService;
    private readonly AtkNodePicker _nodePicker;

    private ushort _selectedAddonId = 0;
    private string _selectedAddonName = string.Empty;
    private bool _sortDirty = true;
    private short _sortColumnIndex = 1;
    private ImGuiSortDirection _sortDirection = ImGuiSortDirection.Ascending;
    private string _addonNameSearchTerm = string.Empty;
    private List<Pointer<AtkResNode>>? _nodePath = null;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("AddonInspectorTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        if (_navigationService.CurrentNavigation is AddonNavigation addonNav)
        {
            _selectedAddonId = addonNav.AddonId;
            _selectedAddonName = addonNav.AddonName ?? string.Empty;
            _nodePath = addonNav.NodePath;
            _navigationService.Reset();
        }

        DrawAddonList();

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        _atkDebugRenderer.DrawInspector(new InspectorContext(_selectedAddonName, _selectedAddonId)
        {
            NodePath = _nodePath
        });

        if (_nodePath != null)
            _nodePath = null;
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
        var showPicker = _nodePicker.ShowPicker;
        if (ImGuiUtils.IconButton("NodeSelectorToggleButton", FontAwesomeIcon.ObjectUngroup, "Pick Addon/Node", active: showPicker))
        {
            _nodePicker.ShowPicker = !showPicker;
            _nodePicker.NodePickerSelectionIndex = 0;
        }

        using var table = ImRaii.Table("AddonsTable"u8, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable, new Vector2(-1));
        if (!table) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.PreferSortDescending, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("DepthLayer"u8, ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.PreferSortDescending, 20);
        ImGui.TableSetupScrollFreeze(0, 1);
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

        allUnitsList.Sort((a, b) =>
        {
            var result = _sortColumnIndex switch
            {
                0 => a.Value->Id.CompareTo(b.Value->Id),
                1 => a.Value->NameString.CompareTo(b.Value->NameString),
                2 => a.Value->DepthLayer.CompareTo(b.Value->DepthLayer) is var r && r != 0 ? r : a.Value->Id.CompareTo(b.Value->Id),
                _ => 0
            };
            return _sortDirection == ImGuiSortDirection.Ascending ? result : -result;
        });

        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
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
            ImGui.Text(addonId.ToString());

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

            ImGuiContextMenu.Draw($"##Addon_{addonId}_{addonName}_Context", builder =>
            {
                var type = _typeService.GetAddonType(addonName);
                var isPinned = _pinnedInstances.Contains(addonName);

                builder.AddCopyName(addonName);
                builder.AddCopyAddress((nint)unitBase);

                builder.AddSeparator();

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == addonName),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new AddonInspectorWindow(_windowManager, _textService, _atkDebugRenderer)
                        {
                            AddonId = addonId,
                            AddonName = addonName
                        });
                    }
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Label = _textService.Translate("ContextMenu.GoToAddressInspector"),
                    ClickCallback = () => _navigationService.NavigateTo(new AddressInspectorNavigation((nint)unitBase, type != typeof(AtkUnitBase) ? (uint)type.SizeOf() : 0))
                });
            });

            ImGui.TableNextColumn(); // DepthLayer
            ImGui.Text(unitBase->DepthLayer.ToString());
        }

        var sortSpecs = ImGui.TableGetSortSpecs();
        _sortDirty |= sortSpecs.SpecsDirty;

        if (!_sortDirty)
            return;

        _sortColumnIndex = sortSpecs.Specs.ColumnIndex;
        _sortDirection = sortSpecs.Specs.SortDirection;
        sortSpecs.SpecsDirty = _sortDirty = false;
    }
}
