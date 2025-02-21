using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient]
public class IndexColumn : ColumnNumber<UnlockLinkEntry>
{
    public IndexColumn()
    {
        SetFixedWidth(60);
        LabelKey = "IndexColumn.Label";
    }

    public override string ToName(UnlockLinkEntry entry)
        => entry.Index.ToString();

    public override int ToValue(UnlockLinkEntry entry)
        => (int)entry.Index;
}
