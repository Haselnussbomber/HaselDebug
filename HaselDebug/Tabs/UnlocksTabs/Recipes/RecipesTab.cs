using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class RecipeTab(RecipesTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Recipes";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count(row => row.RowId < 30000),
            NumUnlocked = table.Rows.Count(row => row.RowId < 30000 && QuestManager.IsRecipeComplete(row.RowId)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
