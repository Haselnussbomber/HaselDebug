using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Fish;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class FishTab(FishTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Fish";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
