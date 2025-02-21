using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Spearfish.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Spearfish;

[RegisterSingleton, AutoConstruct]
public unsafe partial class SpearfishTable : Table<SpearfishingItem>
{
    private readonly ExcelService _excelService;
    private readonly CaughtColumn _caughtColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<SpearfishingItem>.Create(),
            _caughtColumn,
            _nameColumn
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<SpearfishingItem>()
            .Where(row => row.Item.RowId != 0)
            .ToList();
    }
}
