using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<Title>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(Title row)
        => UIState.Instance()->TitleList.IsTitleUnlocked((ushort)row.RowId);
}
