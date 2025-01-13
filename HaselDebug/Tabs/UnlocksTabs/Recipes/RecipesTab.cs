using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class RecipeTab(RecipesTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Recipes";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
