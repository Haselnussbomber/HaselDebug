using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class OrchestrionRollsTab(OrchestrionRollsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Orchestrion Rolls";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(entry => PlayerState.Instance()->IsOrchestrionRollUnlocked(entry.Row.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
