using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class EmotesTab(EmotesTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Emotes";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
