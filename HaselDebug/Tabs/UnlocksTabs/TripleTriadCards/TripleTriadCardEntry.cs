using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards;

public record TripleTriadCardEntry(
    TripleTriadCard Row,
    TripleTriadCardResident ResidentRow,
    Item? Item);
