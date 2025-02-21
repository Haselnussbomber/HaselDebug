using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.SightseeingLog.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

[RegisterSingleton, AutoConstruct]
public unsafe partial class SightseeingLogTable : Table<AdventureEntry>
{
    private readonly ExcelService _excelService;
    private readonly IndexColumn _indexColumn;
    private readonly CompletedColumn _completedColumn;
    private readonly ZoneColumn _zoneColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            EntryRowIdColumn<AdventureEntry, Adventure>.Create(),
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
