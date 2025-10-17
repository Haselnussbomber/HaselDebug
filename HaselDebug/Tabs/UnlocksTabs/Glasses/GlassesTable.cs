using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Glasses.Columns;
using GlassesSheet = Lumina.Excel.Sheets.Glasses;

namespace HaselDebug.Tabs.UnlocksTabs.Glasses;

[RegisterSingleton, AutoConstruct]
public unsafe partial class GlassesTable : Table<GlassesSheet>
{
    private readonly IServiceProvider _serviceProvider;
    internal readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<GlassesSheet>.Create(_serviceProvider),
            _unlockedColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = [.. _excelService.GetSheet<GlassesSheet>().Where(row => row.Icon != 0)];
    }
}
