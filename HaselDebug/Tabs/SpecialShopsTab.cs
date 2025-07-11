using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Tabs.UnlocksTabs;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpecialShopsTab : DebugTab
{
    private readonly SpecialShopsTable _specialShopsTable;
    public override bool DrawInChild => false;

    public override void Draw()
    {
        _specialShopsTable.Draw();
    }
}

[RegisterSingleton, AutoConstruct]
public partial class SpecialShopsTable : Table<SpecialShop>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly SpecialShopsRowColumn _rowColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<SpecialShop>.Create(_serviceProvider),
            _rowColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = [.. _excelService.GetSheet<SpecialShop>()];
        LineHeight = 0;
    }
}

[RegisterSingleton, AutoConstruct]
public partial class SpecialShopsRowColumn : ColumnString<SpecialShop>
{
    private readonly DebugRenderer _debugRenderer;

    public override string ToName(SpecialShop row)
    {
        return row.Name.ExtractText();
    }

    public override void DrawColumn(SpecialShop row)
    {
        _debugRenderer.DrawExdRow(typeof(SpecialShop), row.RowId, 0, new Utils.NodeOptions() { Title = row.Name.ExtractText() });
    }
}
