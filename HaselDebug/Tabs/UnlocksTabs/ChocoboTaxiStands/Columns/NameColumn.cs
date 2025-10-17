using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<ChocoboTaxiStand>
{
    private readonly ISeStringEvaluator _seStringEvaluator;

    [AutoPostConstruct]
    private void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(ChocoboTaxiStand row)
        => row.PlaceName.ToString();

    public override unsafe void DrawColumn(ChocoboTaxiStand row)
    {
        ImGui.Text(ToName(row));

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();

            foreach (var location in row.TargetLocations)
            {
                if (!location.IsValid || !location.Value.Location.IsValid)
                    continue;

                // TODO: TimeRequired and Fare columns are swapped. See https://github.com/xivdev/EXDSchema/pull/103
                ImGui.Text($"→ {_seStringEvaluator.EvaluateFromAddon(102383, [location.Value.Location.RowId, (int)location.Value.TimeRequired, (int)location.Value.Fare])}");
            }
        }
    }
}
