using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class CutscenesTab(CutscenesTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Cutscenes";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
