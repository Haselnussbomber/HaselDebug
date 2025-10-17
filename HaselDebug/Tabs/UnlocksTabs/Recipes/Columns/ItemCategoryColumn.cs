using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;

[RegisterTransient]
public class ItemCategoryColumn : ColumnString<Recipe>
{
    public ItemCategoryColumn()
    {
        SetFixedWidth(275);
    }

    public override string ToName(Recipe row)
        => row.ItemResult.Value.ItemUICategory.Value.Name.ToString();
}
