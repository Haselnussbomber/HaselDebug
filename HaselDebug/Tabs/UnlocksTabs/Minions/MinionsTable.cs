using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Minions.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Minions;

[RegisterSingleton, AutoConstruct]
public unsafe partial class MinionsTable : Table<Companion>
{
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Companion>.Create(),
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
