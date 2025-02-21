using HaselCommon.Extensions.Strings;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;

[RegisterTransient]
public class ItemCategoryColumn : ColumnString<Recipe>
{
    public ItemCategoryColumn()
    {
        SetFixedWidth(275);
    }

    public override string ToName(Recipe row)
        => row.ItemResult.Value.ItemUICategory.Value.Name.ExtractText().StripSoftHypen();
}
