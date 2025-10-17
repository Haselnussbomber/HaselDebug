using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.SightseeingLog.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

[RegisterSingleton, AutoConstruct]
public unsafe partial class SightseeingLogTable : Table<AdventureEntry>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly IndexColumn _indexColumn;
    private readonly CompletedColumn _completedColumn;
    private readonly ZoneColumn _zoneColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            EntryRowIdColumn<AdventureEntry, Adventure>.Create(_serviceProvider),
            _indexColumn,
            _completedColumn,
            _zoneColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Adventure>()
            .Select((row, index) => new AdventureEntry(index, row))
            .ToList();
    }
}
