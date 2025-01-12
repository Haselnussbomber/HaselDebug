using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TitlesTableColumns;

public class RowIdColumn : ColumnNumber<Title>
{
    /*
    public override bool ShouldShow(Title row)
    {
        return row.RowId.ToString().Contains(SearchQuery, StringComparison.InvariantCultureIgnoreCase);
    }
    */

    public override string ToName(Title row)
    {
        return row.RowId.ToString();
    }

    public override int ToValue(Title row)
    {
        return (int)row.RowId;
    }
}
