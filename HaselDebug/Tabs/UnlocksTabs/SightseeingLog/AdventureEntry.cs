using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

public record AdventureEntry(int Index, Adventure Row) : IUnlockEntry
{
    public uint RowId => Row.RowId;
}
