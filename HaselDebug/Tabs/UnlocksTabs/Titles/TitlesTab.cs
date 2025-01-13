using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.Titles;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class TitlesTab(TitlesTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Titles";
    public override bool DrawInChild => !AgentLobby.Instance()->IsLoggedIn;

    public override void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn)
        {
            ImGui.TextUnformatted("Not logged in.");
            return;
        }

        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            ImGui.TextUnformatted("LocalPlayer unavailable.");
            return;
        }

        var uiState = UIState.Instance();
        if (!uiState->TitleList.DataReceived)
        {
            using (ImRaii.Disabled(uiState->TitleList.DataPending))
            {
                if (ImGui.Button("Request Title List"))
                    uiState->TitleList.RequestTitleList();
            }

            return;
        }

        table.Draw();
    }
}
