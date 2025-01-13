using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Minions;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class MinionsTab(MinionsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Minions";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
