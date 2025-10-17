using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog.Columns;

[RegisterTransient, AutoConstruct]
public partial class ZoneColumn : ColumnString<AdventureEntry>
{
    private readonly TextService _textService;
    private readonly IClientState _clientState;
    private readonly MapService _mapService;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(280);
        LabelKey = "ZoneColumn.Label";
    }

    public override string ToName(AdventureEntry entry)
        => _textService.GetPlaceName(entry.Row.PlaceName.RowId);

    public override void DrawColumn(AdventureEntry entry)
    {
        base.DrawColumn(entry);

        var level = entry.Row.Level.Value;
        if (_clientState.TerritoryType == level.Territory.RowId)
        {
            var distance = _mapService.GetDistanceFromPlayer(level);
            if (distance is > 1f and < float.MaxValue)
            {
                var direction = distance > 1 ? " " + _mapService.GetCompassDirection(level) : string.Empty;
                ImGui.SameLine(0, 0);
                ImGui.Text($" ({distance:0}y{direction})");
            }
        }
    }
}
