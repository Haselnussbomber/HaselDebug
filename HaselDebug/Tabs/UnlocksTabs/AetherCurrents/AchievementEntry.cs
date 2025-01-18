using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents;

public record AetherCurrentEntry(AetherCurrentCompFlgSet CompFlgSet, AetherCurrent Row, int Number) : IUnlockEntry
{
    public uint RowId => Row.RowId;
}
