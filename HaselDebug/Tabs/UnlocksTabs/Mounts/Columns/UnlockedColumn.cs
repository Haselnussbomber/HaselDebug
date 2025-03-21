using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Mounts.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<Mount>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(Mount row)
        => PlayerState.Instance()->IsMountUnlocked(row.RowId);
}
