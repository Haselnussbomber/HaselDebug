using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AchievementsTableColumns;

public class RowIdColumn : ColumnNumber<AchievementEntry>
{
    public override string ToName(AchievementEntry entry)
        => entry.Row.RowId.ToString();

    public override int ToValue(AchievementEntry row)
        => (int)row.Row.RowId;
}
