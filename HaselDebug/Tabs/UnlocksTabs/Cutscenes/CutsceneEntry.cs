using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes;

public record CutsceneEntry(
    int Index,
    Cutscene Row,
    CutsceneWorkIndex WorkIndexRow,
    HashSet<(Type SheetType, uint RowId, string Label)> Uses);
