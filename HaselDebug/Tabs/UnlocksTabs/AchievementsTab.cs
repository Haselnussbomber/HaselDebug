using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;
using Achievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;

namespace HaselDebug.Tabs.UnlocksTabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class AchievementsTab(AchievementsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    private bool _achievementsRequested;

    public override string Title => "Achievements";

    public override void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn)
        {
            ImGui.TextUnformatted("Not logged in.");
            return;
        }

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

        if (ImGui.Checkbox("Hide Spoilers", ref table.HideSpoilers))
            table.LoadRows();

        table.Draw();
    }
}
