using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<TripleTriadCardEntry>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(TripleTriadCardEntry entry)
        => UIState.Instance()->IsTripleTriadCardUnlocked((ushort)entry.Row.RowId);
}
