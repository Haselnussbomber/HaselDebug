using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<AchievementEntry>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(AchievementEntry entry)
        => entry.IsComplete;
}
