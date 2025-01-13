using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlockLinksTab(UnlockLinksTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Unlock Links";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
