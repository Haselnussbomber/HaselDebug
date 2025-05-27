using System.Linq;
using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Fish.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Fish;

[RegisterSingleton, AutoConstruct]
public unsafe partial class FishTable : Table<FishParameter>
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly CaughtColumn _caughtColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<FishParameter>.Create(),
            _caughtColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<FishParameter>()
            .Where(row => row.RowId != 0 && row.Item.RowId != 0 && !string.IsNullOrEmpty(_textService.GetItemName(row.Item.RowId).ExtractText().StripSoftHyphen()))
            .ToList();
    }
}
