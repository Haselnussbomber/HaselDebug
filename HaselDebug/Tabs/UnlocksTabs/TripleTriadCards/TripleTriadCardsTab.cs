using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class TripleTriadCardsTab(TripleTriadCardsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Triple Triad Cards";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(entry => UIState.Instance()->IsTripleTriadCardUnlocked((ushort)entry.Row.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
