using System.Data;
using System.Linq;
using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes.Columns;

[RegisterTransient]
public class UsesColumn : ColumnString<CutsceneEntry>
{
    public override string ToName(CutsceneEntry entry)
        => string.Join('\n', entry.Uses.Select(e => $"{e.SheetType.Name}#{e.RowId}{(e.Label.IsNullOrEmpty() ? string.Empty : $" ({e.Label})")}"));
}
