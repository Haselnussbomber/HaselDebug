using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class SightseeingLogTab(SightseeingLogTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Sightseeing Log";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(entry => PlayerState.Instance()->IsAdventureComplete((uint)entry.Index)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
