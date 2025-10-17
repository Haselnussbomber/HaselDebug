using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Fish.Columns;

[RegisterTransient]
public class CaughtColumn : ColumnBool<FishParameter>
{
    public CaughtColumn()
    {
        SetFixedWidth(75);
        LabelKey = "CaughtColumn.Label";
    }

    public override unsafe bool ToBool(FishParameter row)
        => row.IsInLog && PlayerState.Instance()->IsFishCaught(row.RowId);

    public override unsafe void DrawColumn(FishParameter row)
    {
        if (row.IsInLog)
            base.DrawColumn(row);
    }
}
