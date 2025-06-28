using System.Linq;
using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Mounts.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Mounts;

[RegisterSingleton, AutoConstruct]
public unsafe partial class MountsTable : Table<Mount>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Mount>.Create(_serviceProvider),
            _unlockedColumn,
            _nameColumn
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Mount>()
            .Where(row => row.RowId != 0 && row.Order != 0 && row.Icon != 0 && !_textService.GetMountName(row.RowId).IsNullOrWhitespace())
            .ToList();
    }
}
