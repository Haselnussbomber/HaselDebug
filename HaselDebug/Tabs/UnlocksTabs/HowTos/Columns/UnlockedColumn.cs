using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<HowTo>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(HowTo row)
        => UIState.Instance()->IsHowToUnlocked(row.RowId);
}
