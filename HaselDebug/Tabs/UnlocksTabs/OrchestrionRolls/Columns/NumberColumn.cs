using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls.Columns;

[RegisterTransient]
public class NumberColumn : ColumnString<OrchestrionRollEntry>
{
    public NumberColumn()
    {
        SetFixedWidth(75);
    }

    public override string ToName(OrchestrionRollEntry entry)
        => entry.UIParamRow.Order == 65535 ? "\u2014" : $"{entry.UIParamRow.Order:000}";
}
