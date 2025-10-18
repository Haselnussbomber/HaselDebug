using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<UnlockLinkEntry>
{
    public UnlockedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(UnlockLinkEntry entry)
        => UIState.Instance()->IsUnlockLinkUnlocked((ushort)entry.Index);
}
