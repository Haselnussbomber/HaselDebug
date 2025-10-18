using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog.Columns;

[RegisterTransient]
public class CompletedColumn : ColumnYesNo<AdventureEntry>
{
    public CompletedColumn()
    {
        SetFixedWidth(75);
        LabelKey = "CompletedColumn.Label";
    }

    public override unsafe bool ToBool(AdventureEntry entry)
        => PlayerState.Instance()->IsAdventureComplete((uint)entry.Index);
}
