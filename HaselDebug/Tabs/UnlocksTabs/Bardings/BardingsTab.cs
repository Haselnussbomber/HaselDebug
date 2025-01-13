using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Bardings;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class BardingsTab(BardingsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Bardings";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
