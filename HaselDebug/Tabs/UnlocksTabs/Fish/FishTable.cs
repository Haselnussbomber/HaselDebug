using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Fish.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Fish;

[RegisterSingleton, AutoConstruct]
public unsafe partial class FishTable : Table<FishParameter>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly CaughtColumn _caughtColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<FishParameter>.Create(_serviceProvider),
            _caughtColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<FishParameter>()
            .Where(row => row.RowId != 0 && row.Item.RowId != 0 && !string.IsNullOrEmpty(_textService.GetItemName(row.Item.RowId).ToString()))
            .ToList();
    }
}
