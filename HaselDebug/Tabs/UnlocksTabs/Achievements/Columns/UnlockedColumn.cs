using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;

public class UnlockedColumn : ColumnBool<AchievementEntry>
{
    public override unsafe bool ToBool(AchievementEntry entry)
        => entry.IsComplete;
}
