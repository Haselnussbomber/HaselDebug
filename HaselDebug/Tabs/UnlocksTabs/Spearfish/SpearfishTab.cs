using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Spearfish;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class SpearfishTab(SpearfishTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Spearfish";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
