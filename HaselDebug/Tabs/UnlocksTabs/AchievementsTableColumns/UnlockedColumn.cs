using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AchievementsTableColumns;

public class UnlockedColumn : ColumnBool<AchievementEntry>
{
    public override unsafe bool ToBool(AchievementEntry entry)
    {
        return entry.IsComplete;
    }
}
