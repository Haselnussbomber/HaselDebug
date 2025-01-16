using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Quests;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class QuestsTab(QuestsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Quests";

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(row => UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted((ushort)row.RowId + 0x10000u)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
