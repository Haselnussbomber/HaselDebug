using System.Linq;
using HaselCommon.Game.Enums;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.TripleTriadCards.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards;

[RegisterSingleton, AutoConstruct]
public unsafe partial class TripleTriadCardsTable : Table<TripleTriadCardEntry>
{
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly NumberColumn _numberColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            EntryRowIdColumn<TripleTriadCardEntry, TripleTriadCard>.Create(),
            _unlockedColumn,
            _numberColumn,
            _nameColumn,
        ];
    }

    public override void LoadRows()
    {
        var residentSheet = _excelService.GetSheet<TripleTriadCardResident>();
        var obtainSheet = _excelService.GetSheet<TripleTriadCardObtain>();

        var cardItems = _excelService.GetSheet<Item>()
            .Where(itemRow => itemRow.ItemAction.Value.Type == (uint)ItemActionType.TripleTriadCard)
            .ToDictionary(itemRow => (uint)itemRow.ItemAction.Value!.Data[0]);

        Rows = _excelService.GetSheet<TripleTriadCard>()
            .Where(row => row.RowId != 0 && residentSheet.HasRow(row.RowId) && cardItems.ContainsKey(row.RowId))
            .Select(row =>
            {
                var residentRow = residentSheet.GetRow(row.RowId);
                var obtainRow = obtainSheet.GetRow(residentRow.AcquisitionType.RowId);
                return new TripleTriadCardEntry(row, residentRow, obtainRow.Icon, cardItems[row.RowId]);
            })
            .ToList();
    }
}
