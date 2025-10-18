using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Bardings.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<BuddyEquip>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(BuddyEquip row)
        => UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(row.RowId);
}
