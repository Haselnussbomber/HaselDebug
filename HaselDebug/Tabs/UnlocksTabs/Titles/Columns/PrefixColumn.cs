using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

[RegisterTransient]
public class PrefixColumn : ColumnBool<Title>
{
    public PrefixColumn()
    {
        SetFixedWidth(75);
    }

    public override bool ToBool(Title row)
        => row.IsPrefix;
}
