namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes;

public record CutsceneEntry(
    int Index,
    Cutscene Row,
    CutsceneWorkIndex WorkIndexRow,
    HashSet<(Type SheetType, uint RowId, string Label)> Uses) : IUnlockEntry
{
    public uint RowId => Row.RowId;
}
