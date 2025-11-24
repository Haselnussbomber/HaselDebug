using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes;

[RegisterSingleton, AutoConstruct]
public unsafe partial class RecipesTable : Table<Recipe>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly PatchColumn _patchColumn;
    private readonly CompletedColumn _completedColumn;
    private readonly ItemCategoryColumn _itemCategoryColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Recipe>.Create(_serviceProvider),
            _completedColumn,
            _patchColumn,
            _itemCategoryColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = [.. _excelService.GetSheet<Recipe>().Where(row => row.ItemResult.RowId > 0)];
    }
}
