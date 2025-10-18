using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Items.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Items;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ItemsTable : Table<Item>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly TypeColumn _typeColumn;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly ItemColumn _itemColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Item>.Create(_serviceProvider),
            _typeColumn,
            _unlockedColumn,
            _itemColumn
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Item>()
            .Where(row => new ItemHandle(row.RowId).IsUnlockable)
            .ToList();
    }
}
