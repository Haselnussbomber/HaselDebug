using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<AchievementEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(AchievementEntry entry)
        => entry.Name;

    public override unsafe void DrawColumn(AchievementEntry entry)
    {
        _debugRenderer.DrawIcon(entry.Row.Icon);

        var canClick = entry.CanShowName && entry.CanShowCategory;
        var clicked = false;
        using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canClick))
        using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canClick))
            clicked = ImGui.Selectable(entry.Name);

        if (canClick && clicked)
            AgentAchievement.Instance()->OpenById(entry.Row.RowId);

        if (ImGui.IsItemHovered())
        {
            if (canClick)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _unlocksTabUtils.DrawTooltip(entry.Row.Icon, entry.Name, entry.CategoryName, entry.Description);
        }
    }
}
