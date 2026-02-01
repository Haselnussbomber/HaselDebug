using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Items.Columns;

[RegisterTransient]
public class UnlockedColumn : ColumnYesNo<Item>
{
    private readonly ItemService _itemService;

    public UnlockedColumn(ItemService itemService)
    {
        _itemService = itemService;

        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override unsafe bool ToBool(Item row)
        => _itemService.IsUnlocked(row);
}
