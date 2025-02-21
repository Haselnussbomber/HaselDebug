using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Emotes.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes;

[RegisterSingleton, AutoConstruct]
public unsafe partial class EmotesTable : Table<Emote>
{
    private readonly ExcelService _excelService;
    private readonly CanUseColumn _canUseColumn;
    private readonly ItemColumn _itemColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Emote>.Create(),
            _canUseColumn,
            _itemColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Emote>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty)
            .ToList();
    }
}
