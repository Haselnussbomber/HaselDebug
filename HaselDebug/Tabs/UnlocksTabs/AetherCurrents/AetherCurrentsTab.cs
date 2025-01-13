using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class AetherCurrentsTab(AetherCurrentsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Aether Currents";

    public override void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn)
        {
            ImGui.TextUnformatted("Not logged in.");
            return;
        }

        table.Draw();
    }
}
