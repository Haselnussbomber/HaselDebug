using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Minions.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Minions;

[RegisterSingleton, AutoConstruct]
public unsafe partial class MinionsTable : Table<Companion>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Companion>.Create(_serviceProvider),
            _unlockedColumn,
            _nameColumn
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Companion>()
            .Where(row => row.RowId != 0 && row.Order != 0)
            .ToList();
    }
}
