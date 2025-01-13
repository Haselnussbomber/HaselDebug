using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;

public class CategoryColumn : ColumnString<AchievementEntry>
{
    public override string ToName(AchievementEntry entry)
        => entry.CategoryName;
}
