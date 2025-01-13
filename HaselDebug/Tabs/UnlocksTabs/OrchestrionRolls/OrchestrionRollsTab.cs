using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class OrchestrionRollsTab(OrchestrionRollsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Orchestrion Rolls";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
