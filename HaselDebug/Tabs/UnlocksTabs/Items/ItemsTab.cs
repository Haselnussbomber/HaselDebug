using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Items;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class ItemsTab(ItemsTable table, ItemService itemService) : DebugTab, IUnlockTab
{
    public override string Title => "Items";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count(row => itemService.IsUnlockable(row)),
            NumUnlocked = table.Rows.Count(row => itemService.IsUnlocked(row)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
