using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using ImGuiNET;
using Achievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using AchievementSheet = Lumina.Excel.Sheets.Achievement;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    private bool AchievementsRequested;
    private bool AchievementsHideSpoilers = true;

    public void DrawAchievements()
    {
        using var tab = ImRaii.TabItem("Achievements");
        if (!tab) return;

        var achievement = Achievement.Instance();
        if (!achievement->IsLoaded())
        {
            using (ImRaii.Disabled(achievement->ProgressRequestState == Achievement.AchievementState.Requested))
            {
                if (ImGui.Button("Request Achievements List"))
                {
                    AgentAchievement.Instance()->Show();
                    AchievementsRequested = true;
                }
            }

            return;
        }

        if (AchievementsRequested)
        {
            AgentAchievement.Instance()->Hide();
            AchievementsRequested = false;
        }

        ImGui.Checkbox("Hide Spoilers", ref AchievementsHideSpoilers);

        using var table = ImRaii.Table("AchievementsTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<AchievementSheet>())
        {
            if (row.RowId == 0 || row.AchievementCategory.RowId == 0 || !row.AchievementCategory.IsValid || !row.AchievementHideCondition.IsValid)
                continue;

            var isUnlocked = achievement->IsComplete((int)row.RowId);

            if (AchievementsHideSpoilers && !isUnlocked && (row.AchievementCategory.Value.HideCategory || row.AchievementHideCondition.Value.HideAchievement))
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Category
            var categoryName = row.AchievementCategory.Value.Name.ExtractText().StripSoftHypen();
            ImGui.TextUnformatted(categoryName);

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(row.Icon);

            var name = (AchievementsHideSpoilers && !isUnlocked && row.AchievementHideCondition.Value.HideName)
                ? "???"
                : row.Name.ExtractText().StripSoftHypen();

            var hideConditions = AchievementsHideSpoilers && !isUnlocked && row.AchievementHideCondition.Value.HideConditions;
            var clicked = false;
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, hideConditions))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, hideConditions))
                clicked = ImGui.Selectable(name);

            if (clicked)
            {
                AgentAchievement.Instance()->OpenById(row.RowId);
            }

            if (ImGui.IsItemHovered())
            {
                if (!hideConditions)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                using var tooltip = ImRaii.Tooltip();
                if (!tooltip) continue;

                using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
                if (!popuptable) continue;

                ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 + ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, 300);

                ImGui.TableNextColumn(); // Icon
                TextureService.DrawIcon(row.Icon, 40);

                ImGui.TableNextColumn(); // Text
                using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, ImGui.GetStyle().ItemInnerSpacing.X);
                using var indent = ImRaii.PushIndent(1);

                ImGui.TextUnformatted(name);
                ImGuiUtils.PushCursorY(-3);
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                    ImGui.TextUnformatted(categoryName);
                ImGuiUtils.PushCursorY(1);

                // separator
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
                ImGuiUtils.PushCursorY(4);

                ImGuiHelpers.SafeTextWrapped(row.Description.ExtractText().StripSoftHypen());
            }
        }
    }
}
