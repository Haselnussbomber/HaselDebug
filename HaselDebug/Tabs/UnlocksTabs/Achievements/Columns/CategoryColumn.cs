using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;

[RegisterTransient]
public class CategoryColumn : ColumnString<AchievementEntry>
{
    public CategoryColumn()
    {
        SetFixedWidth(165);
        LabelKey = "CategoryColumn.Label";
    }

    public override string ToName(AchievementEntry entry)
        => entry.CategoryName;
}
