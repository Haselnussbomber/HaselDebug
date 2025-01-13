using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

public class RowIdColumn : ColumnNumber<Title>
{
    public override string ToName(Title row)
        => row.RowId.ToString();

    public override int ToValue(Title row)
        => (int)row.RowId;
}
