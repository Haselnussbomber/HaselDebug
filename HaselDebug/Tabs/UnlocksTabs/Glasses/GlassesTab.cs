using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Glasses;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class GlassesTab(GlassesTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Glasses";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
