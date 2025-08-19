using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class AetherCurrentsTab(AetherCurrentsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Aether Currents";
    public override bool DrawInChild => !AgentLobby.Instance()->IsLoggedIn;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(entry => PlayerState.Instance()->IsAetherCurrentUnlocked(entry.Row.RowId)),
        };
    }

    public override void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn)
        {
            ImGui.Text("Not logged in.");
            return;
        }

        table.Draw();
    }
}
