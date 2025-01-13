using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Mounts;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class MountsTab(MountsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Mounts";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
