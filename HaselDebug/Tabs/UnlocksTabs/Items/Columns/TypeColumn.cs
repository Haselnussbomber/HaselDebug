using HaselCommon.Game.Enums;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Items.Columns;

[RegisterTransient]
public partial class TypeColumn : ColumnString<Item>
{
    public TypeColumn()
    {
        SetFixedWidth(120);
    }

    public override string ToName(Item row)
        => ((ItemActionType)row.ItemAction.Value.Type).ToString();
}
