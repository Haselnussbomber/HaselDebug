using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.GatheringItems.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.GatheringItems;

[RegisterSingleton, AutoConstruct]
public partial class GatheringItemsTable : Table<GatheringItem>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly GatheredColumn _gatheredColumn;
    private readonly TypeColumn _typeColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<GatheringItem>.Create(_serviceProvider),
            _gatheredColumn,
            _typeColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<GatheringItem>()
            .Where(row => row.RowId != 0 && row.Item.RowId != 0 && !string.IsNullOrEmpty(_textService.GetItemName(row.Item.RowId).ToString()))
            .ToList();
    }
}
