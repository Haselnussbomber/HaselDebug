using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;

[RegisterTransient]
public class CompletedColumn : ColumnYesNo<Recipe>
{
    public CompletedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "CompletedColumn.Label";
    }

    public override unsafe bool ToBool(Recipe row)
        => row.RowId < 30000 && QuestManager.IsRecipeComplete(row.RowId);

    public override void DrawColumn(Recipe row)
    {
        if (row.RowId < 30000)
            base.DrawColumn(row);
    }
}
