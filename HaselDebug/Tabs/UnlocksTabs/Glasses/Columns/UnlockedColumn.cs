using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using GlassesSheet = Lumina.Excel.Sheets.Glasses;

namespace HaselDebug.Tabs.UnlocksTabs.Glasses.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<GlassesSheet>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(GlassesSheet row)
        => PlayerState.Instance()->IsGlassesUnlocked((ushort)row.RowId);
}
