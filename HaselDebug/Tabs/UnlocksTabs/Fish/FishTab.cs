using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Fish;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class FishTab(FishTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Fish";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count(row => row.IsInLog),
            NumUnlocked = table.Rows.Count(row => row.IsInLog && PlayerState.Instance()->IsFishCaught(row.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
