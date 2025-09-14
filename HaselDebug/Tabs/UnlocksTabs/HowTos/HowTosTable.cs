using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.HowTos;

[RegisterSingleton, AutoConstruct]
public unsafe partial class HowTosTable : Table<HowTo>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<HowTo>.Create(_serviceProvider),
            _unlockedColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = [.. _excelService
            .GetSheet<HowTo>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty)];
    }
}
