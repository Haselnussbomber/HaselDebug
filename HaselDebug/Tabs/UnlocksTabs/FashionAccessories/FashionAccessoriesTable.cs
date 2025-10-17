using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.FashionAccessories.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.FashionAccessories;

[RegisterSingleton, AutoConstruct]
public unsafe partial class FashionAccessoriesTable : Table<Ornament>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Ornament>.Create(_serviceProvider),
            _unlockedColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Ornament>()
            .Where(row => row.RowId is not (0 or 22 or 25 or 26 or 32) && row.Order != 0 && row.Model != 0 && row.Icon != 0) // see AgentOrnamentNoteBook_Show
            .ToList();
    }
}
