using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AdventuresTable : Table<ChocoboTaxiStand>
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
            .Where(row => row.RowId != 0 && !row.PlaceName.IsEmpty)];
    }
}
