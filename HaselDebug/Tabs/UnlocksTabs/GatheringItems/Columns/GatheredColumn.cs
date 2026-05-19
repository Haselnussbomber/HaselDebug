using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.GatheringItems.Columns;

[RegisterTransient]
public class GatheredColumn : ColumnYesNo<GatheringItem>
{
    public GatheredColumn()
    {
        SetFixedWidth(75);
        LabelKey = "GatheredColumn.Label";
    }

    public override unsafe bool ToBool(GatheringItem row)
        =>  QuestManager.IsGatheringItemGathered((ushort)row.RowId);

    public override void DrawColumn(GatheringItem row)
    {
        if (row.RowId < 10000 && row.Item.Is<Item>())
            base.DrawColumn(row);
    }
}
