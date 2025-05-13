using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class AozActionsTab(AozActionsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Blue Mage Actions";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(row => UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(row.Action.UnlockLink.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
