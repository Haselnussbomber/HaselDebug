using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Items.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnBool<Item>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(Item row)
        => new ItemHandle(row.RowId).IsUnlocked;
}
