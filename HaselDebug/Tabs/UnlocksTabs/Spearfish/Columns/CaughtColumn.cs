using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Spearfish.Columns;

[RegisterTransient]
public class CaughtColumn : ColumnBool<SpearfishingItem>
{
    public CaughtColumn()
    {
        SetFixedWidth(75);
        LabelKey = "CaughtColumn.Label";
    }

    public override unsafe bool ToBool(SpearfishingItem row)
        => row.IsVisible && PlayerState.Instance()->IsSpearfishCaught(row.RowId);

    public override void DrawColumn(SpearfishingItem row)
    {
        if (row.IsVisible)
            base.DrawColumn(row);
    }
}
