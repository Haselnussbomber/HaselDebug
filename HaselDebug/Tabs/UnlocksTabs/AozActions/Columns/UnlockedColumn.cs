using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<AozEntry>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(AozEntry entry)
        => UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(entry.Action.UnlockLink.RowId);
}
