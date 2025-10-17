using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ChocoboTaxiStandsTable : Table<ChocoboTaxiStand>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<ChocoboTaxiStand>.Create(_serviceProvider),
            _unlockedColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = [.. _excelService
            .GetSheet<ChocoboTaxiStand>()
            .Where(row => row.RowId is not (0 or 1179648 or 1179649 or 1179678) && !row.PlaceName.IsEmpty)];
    }
}
