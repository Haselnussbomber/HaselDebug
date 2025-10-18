using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<OrchestrionRollEntry>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(OrchestrionRollEntry entry)
        => PlayerState.Instance()->IsOrchestrionRollUnlocked(entry.Row.RowId);
}
