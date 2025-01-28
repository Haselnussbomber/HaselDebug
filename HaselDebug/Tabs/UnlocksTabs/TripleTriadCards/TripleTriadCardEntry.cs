using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards;

public record TripleTriadCardEntry(
    TripleTriadCard Row,
    TripleTriadCardResident ResidentRow,
    uint UnlockIcon,
    Item? Item) : IUnlockEntry
{
    public uint RowId => Row.RowId;
}
