using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<HowTo>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(HowTo row)
        => UIState.Instance()->IsHowToUnlocked(row.RowId);
}
