using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class EmotesTab(EmotesTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Emotes";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(row => row.UnlockLink == 0 || UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted(row.UnlockLink)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
