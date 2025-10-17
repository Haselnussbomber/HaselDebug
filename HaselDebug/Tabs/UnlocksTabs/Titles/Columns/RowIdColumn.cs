using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

[RegisterTransient]
public class RowIdColumn : ColumnNumber<Title>
{
    public RowIdColumn()
    {
        LabelKey = "RowIdColumn.Label";
    }

    public override string ToName(Title row)
        => row.RowId.ToString();

    public override int ToValue(Title row)
        => (int)row.RowId;
}
