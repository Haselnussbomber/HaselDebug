using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Achievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using AchievementSheet = Lumina.Excel.Sheets.Achievement;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabAchievements(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    UnlocksTabUtils UnlocksTabUtils) : DebugTab, ISubTab<UnlocksTab>
{
    private bool _achievementsRequested;
    private bool _achievementsHideSpoilers = true;

    public override string Title => "Achievements";

    public override void Draw()
    {
        var achievement = Achievement.Instance();
        if (!achievement->IsLoaded())
        {
            using (ImRaii.Disabled(achievement->ProgressRequestState == Achievement.AchievementState.Requested))
            {
                if (ImGui.Button("Request Achievements List"))
                {
                    AgentAchievement.Instance()->Show();
                    _achievementsRequested = true;
                }
            }

            return;
        }

        if (_achievementsRequested)
        {
            AgentAchievement.Instance()->Hide();
            _achievementsRequested = false;
        }

        ImGui.Checkbox("Hide Spoilers", ref _achievementsHideSpoilers);

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

            var isComplete = achievement->IsComplete((int)row.RowId);

            var canShow = !_achievementsHideSpoilers || isComplete;

            var isHiddenName = row.AchievementHideCondition.Value.HideName == true;
            var isHiddenCategory = row.AchievementCategory.Value.HideCategory == true;
            var isHiddenAchievement = row.AchievementHideCondition.Value.HideAchievement == true;

            var canShowName = canShow || !isHiddenName;
            var canShowCategory = canShow || !isHiddenCategory;
            var canShowDescription = canShow || (!isHiddenName && !isHiddenCategory && !isHiddenAchievement); // idk actually
            var canShowAchievement = canShow || !isHiddenAchievement;

            if (!canShowAchievement)
                continue;

            var name = canShowName ? row.Name.ExtractText().StripSoftHypen() : "???";
            var categoryName = canShowCategory ? row.AchievementCategory.Value.Name.ExtractText().StripSoftHypen() : "???";
            var description = canShowDescription ? row.Description.ExtractText().StripSoftHypen() : "???";

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isComplete ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isComplete.ToString());

            ImGui.TableNextColumn(); // Category
            ImGui.TextUnformatted(categoryName);

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(row.Icon);

            var canClick = canShowName && canShowCategory;
            var clicked = false;
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canClick))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canClick))
                clicked = ImGui.Selectable(name);

            if (canClick && clicked)
            {
                AgentAchievement.Instance()->OpenById(row.RowId);
            }

            if (ImGui.IsItemHovered())
            {
                if (canClick)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                UnlocksTabUtils.DrawTooltip((uint)row.Icon, name, categoryName, description);
            }
        }
    }
}
