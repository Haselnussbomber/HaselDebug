using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

public class PrefixColumn : ColumnBool<Title>
{
    public override bool ToBool(Title row)
    {
        return row.IsPrefix;
    }
}
