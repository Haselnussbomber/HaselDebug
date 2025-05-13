using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions.Columns;

[RegisterTransient]
public class NumberColumn : ColumnNumber<AozEntry>
{
    public NumberColumn()
    {
        SetFixedWidth(60);
    }

    public override int ToValue(AozEntry entry)
        => entry.AozActionTransient.Number;
}
