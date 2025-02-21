using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Bardings.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<BuddyEquip>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(BuddyEquip row)
        => UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(row.RowId);
}
