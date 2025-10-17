using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.AozActions.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AozActionsTable : Table<AozEntry>
{
    private readonly ExcelService _excelService;
    private readonly NumberColumn _numberColumn;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly ActionColumn _actionColumn;
    private readonly LocationColumn _locationColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            _numberColumn,
            _unlockedColumn,
            _actionColumn,
            _locationColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = [];

        foreach (var row in _excelService.GetSheet<AozAction>())
        {
            if (row.RowId == 0)
                continue;

            if (!_excelService.TryGetRow<AozActionTransient>(row.RowId, out var transient))
                continue;

            if (!row.Action.IsValid)
                continue;

            Rows.Add(new AozEntry(row, transient));
        }
    }
}
