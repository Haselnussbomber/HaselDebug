using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes;

[RegisterSingleton, AutoConstruct]
public unsafe partial class RecipesTable : Table<Recipe>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly CompletedColumn _completedColumn;
    private readonly ItemCategoryColumn _itemCategoryColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Recipe>.Create(_serviceProvider),
            _completedColumn,
            _itemCategoryColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Recipe>()
            .Where(row => row.ItemResult.RowId > 0)
            .ToList();
    }
}
