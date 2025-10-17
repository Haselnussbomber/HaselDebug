using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls;

[RegisterSingleton, AutoConstruct]
public unsafe partial class OrchestrionRollsTable : Table<OrchestrionRollEntry>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly CategoryColumn _categoryColumn;
    private readonly NumberColumn _numberColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            EntryRowIdColumn<OrchestrionRollEntry, Orchestrion>.Create(_serviceProvider),
            _unlockedColumn,
            _categoryColumn,
            _numberColumn,
            _nameColumn
        ];

        // TODO: Flags |= ImGuiTableFlags.SortMulti;
    }

    public override void LoadRows()
    {
        var uiParamSheet = _excelService.GetSheet<OrchestrionUiparam>();
        Rows = _excelService.GetSheet<Orchestrion>()
            .Where(row => row.RowId != 0 && uiParamSheet.HasRow(row.RowId))
            .Select(row =>
            {
                _excelService.TryGetRow<OrchestrionUiparam>(row.RowId, out var uiParam);
                return new OrchestrionRollEntry(row, uiParam);
            })
            .Where(entry => entry.UIParamRow.OrchestrionCategory.RowId != 0 && entry.UIParamRow.OrchestrionCategory.IsValid)
            .ToList();
    }
}
