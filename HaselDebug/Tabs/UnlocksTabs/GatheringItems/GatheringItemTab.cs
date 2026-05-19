using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.GatheringItems;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public class GatheringItemsTab(GatheringItemsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Gathering Items";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count(row => row.RowId < 10000 && row.Item.Is<Item>()),
            NumUnlocked = table.Rows.Count(row => row.RowId < 10000 && row.Item.Is<Item>() && QuestManager.IsGatheringItemGathered((ushort)row.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
