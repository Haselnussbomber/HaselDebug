using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog.Columns;

[RegisterTransient]
public class IndexColumn : ColumnNumber<AdventureEntry>
{
    public IndexColumn()
    {
        SetFixedWidth(50);
        LabelKey = "IndexColumn.Label";
    }

    public override string ToName(AdventureEntry entry)
        => entry.Index.ToString();

    public override int ToValue(AdventureEntry entry)
        => entry.Index;
}
