using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;

namespace HaselDebug.Tabs.UnlocksTabs.GatheringItems.Columns;

[RegisterTransient, AutoConstruct]
public partial class TypeColumn : Column<GatheringItem>
{
    private readonly ItemService _itemService;
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;

    private GatheringTypeFlag _selectedType = GatheringTypeFlag.All;

    [AutoPostConstruct]
    private void Initialize()
    {
        SetFixedWidth(75);
        LabelKey = "TypeColumn.Label";
    }

    public virtual GatheringTypeFlag ToType(GatheringItem row)
    {
        if (row.RowId < 10000 && row.Item.TryGetValue<Item>(out var item) && _itemService.GetGatheringPoints(item).TryGetFirst(out var point))
            return (GatheringTypeFlag)point.GatheringPointBase.Value.GatheringType.RowId;

        return GatheringTypeFlag.All;
    }

    public override bool ShouldShow(GatheringItem row)
    {
        var type = ToType(row);
        return type == _selectedType || _selectedType == GatheringTypeFlag.All;
    }

    public override void DrawColumn(GatheringItem row)
    {
        if (row.Item.TryGetValue<Item>(out var item) && _itemService.GetGatheringPoints(item).TryGetFirst(out var point))
        {
            var gatheringType = point.GatheringPointBase.Value.GatheringType.Value;
            var rare = !UIGlobals.IsExportedGatheringPointTimed(point.Type);
            _debugRenderer.DrawIcon(rare ? (uint)gatheringType.IconMain : (uint)gatheringType.IconOff);
        }
    }

    public string GetTranslatedName(GatheringTypeFlag type)
        => _textService.TryGetTranslation($"HaselDebug.Tabs.UnlocksTabs.GatheringItems.Columns.TypeColumn.{type}", out var text) ? text : type.ToString();

    public override int Compare(GatheringItem a, GatheringItem b)
        => ToType(a).CompareTo(ToType(b));

    public override bool DrawFilter()
    {
        using var id = ImRaii.PushId("##Filter");
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(-Table.ArrowWidth * ImStyle.Scale);
        var all = _selectedType == GatheringTypeFlag.All;
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0x803030A0, !all);
        using var combo = ImRaii.Combo(string.Empty, Label, ImGuiComboFlags.NoArrowButton);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _selectedType = GatheringTypeFlag.All;
            return true;
        }

        var textService = ServiceLocator.GetService<TextService>();

        if (!all && ImGui.IsItemHovered())
            ImGui.SetTooltip(textService?.Translate("ImGuiTable.ColumnFlags.Filter.RightClickToClear") ?? "Right-click to clear filters.");

        if (!combo)
            return false;

        color.Pop();

        var ret = ImGui.RadioButton($"{GetTranslatedName(GatheringTypeFlag.All)}##GatheringType", ref _selectedType, GatheringTypeFlag.All);

        for (var i = 0; i < 4; ++i)
        {
            ret |= ImGui.RadioButton($"{GetTranslatedName((GatheringTypeFlag)i)}##GatheringType", ref _selectedType, (GatheringTypeFlag)i);
        }

        return ret;
    }
}

public enum GatheringTypeFlag
{
    All = -1,
    Mining,
    Quarrying,
    Logging,
    Harvesting
}
