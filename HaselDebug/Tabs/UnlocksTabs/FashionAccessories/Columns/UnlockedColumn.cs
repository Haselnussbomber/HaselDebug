using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.FashionAccessories.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<Ornament>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(Ornament row)
        => PlayerState.Instance()->IsOrnamentUnlocked(row.RowId);
}
