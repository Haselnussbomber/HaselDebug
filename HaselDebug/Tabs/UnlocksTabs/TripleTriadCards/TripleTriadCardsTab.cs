using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class TripleTriadCardsTab(TripleTriadCardsTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Triple Triad Cards";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
