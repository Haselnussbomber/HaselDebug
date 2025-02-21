using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Minions.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<Companion>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(Companion row)
        => UIState.Instance()->IsCompanionUnlocked(row.RowId);
}
