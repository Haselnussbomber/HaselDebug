using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AchievementsTableColumns;

public class CategoryColumn : ColumnString<AchievementEntry>
{
    public override string ToName(AchievementEntry entry)
    {
        return entry.CategoryName;
    }
}
