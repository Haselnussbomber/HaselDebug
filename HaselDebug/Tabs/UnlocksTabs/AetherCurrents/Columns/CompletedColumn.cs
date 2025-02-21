using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents.Columns;

[RegisterTransient]
public class CompletedColumn : ColumnBool<AetherCurrentEntry>
{
    public CompletedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "CompletedColumn.Label";
    }

    public override unsafe bool ToBool(AetherCurrentEntry entry)
        => PlayerState.Instance()->IsAetherCurrentUnlocked(entry.Row.RowId);
}
