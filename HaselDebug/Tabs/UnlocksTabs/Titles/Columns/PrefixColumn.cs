using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

[RegisterTransient]
public class PrefixColumn : ColumnYesNo<Title>
{
    public PrefixColumn()
    {
        SetFixedWidth(75);
    }

    public override bool ToBool(Title row)
        => row.IsPrefix;
}
