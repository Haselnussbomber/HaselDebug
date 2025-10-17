using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;

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
