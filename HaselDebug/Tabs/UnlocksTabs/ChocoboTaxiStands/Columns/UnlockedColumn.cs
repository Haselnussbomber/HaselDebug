using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<ChocoboTaxiStand>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(ChocoboTaxiStand row)
        => UIState.Instance()->IsChocoboTaxiStandUnlocked(row.RowId);
}
