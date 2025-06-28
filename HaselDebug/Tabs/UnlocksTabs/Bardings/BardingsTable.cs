using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Bardings.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Bardings;

[RegisterSingleton, AutoConstruct]
public unsafe partial class BardingsTable : Table<BuddyEquip>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly ItemColumn _itemColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<BuddyEquip>.Create(_serviceProvider),
            _unlockedColumn,
            _itemColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<BuddyEquip>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty)
            .ToList();
    }
}
