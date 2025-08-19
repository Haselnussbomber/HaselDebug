using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents.Columns;

[RegisterTransient]
public class ZoneColumn : ColumnString<AetherCurrentEntry>
{
    public ZoneColumn()
    {
        SetFixedWidth(235);
        LabelKey = "ZoneColumn.Label";
    }

    public override string ToName(AetherCurrentEntry entry)
        => entry.CompFlgSet.Territory.Value.Map.Value.PlaceName.Value.Name.ToString();
}
