namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls;

public record OrchestrionRollEntry(
    Orchestrion Row,
    OrchestrionUiparam UIParamRow) : IUnlockEntry
{
    public uint RowId => Row.RowId;
}
