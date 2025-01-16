using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Spearfish;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class SpearfishTab(SpearfishTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Spearfish";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count(row => row.IsVisible),
            NumUnlocked = table.Rows.Count(row => row.IsVisible && PlayerState.Instance()->IsSpearfishCaught(row.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
