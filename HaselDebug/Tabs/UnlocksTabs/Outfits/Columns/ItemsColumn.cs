using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Sheets;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton, AutoConstruct]
public partial class ItemsColumn : ColumnString<CustomMirageStoreSetItem>
{
    private const float IconSize = OutfitsTable.IconSize;

    private readonly ITextureProvider _textureProvider;
    private readonly TextService _textService;
    private readonly ImGuiContextMenuService _imGuiContextMenuService;
    private readonly ExcelService _excelService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    private readonly StringBuilder _stringBuilder = new();

    [AutoPostConstruct]
    public void Initialize()
    {
        Flags |= ImGuiTableColumnFlags.NoSort;
    }

    public override string ToName(CustomMirageStoreSetItem row)
    {
        _stringBuilder.Clear();

        for (var i = 1; i < row.Items.Count; i++)
            _stringBuilder.AppendLine(_textService.GetItemName(row.Items[i].RowId).ToString());

        return _stringBuilder.ToString();
    }

    public override unsafe void DrawColumn(CustomMirageStoreSetItem row)
    {
        var itemFinderModule = ItemFinderModule.Instance();
        var glamourDresserItemIds = itemFinderModule->GlamourDresserItemIds;
        var glamourDresserItemSetUnlockBits = itemFinderModule->GlamourDresserItemSetUnlockBits;
        var glamourDresserIndex = glamourDresserItemIds.IndexOf(row.RowId);
        var hasSetItem = glamourDresserIndex != -1;

        var isSetCollected = hasSetItem;
        if (hasSetItem)
        {
            var unlockBitArray = new BitArray((byte*)glamourDresserItemSetUnlockBits.GetPointer(glamourDresserIndex), row.Items.Count);
            for (var slotIndex = 0; slotIndex < row.Items.Count; slotIndex++)
            {
                var slotItem = row.Items[slotIndex];
                if (slotItem.RowId == 0)
                    continue;

                isSetCollected &= unlockBitArray.TryGet(slotIndex, out var slotLocked) && !slotLocked;
            }
        }

        for (var slotIndex = 0; slotIndex < row.Items.Count; slotIndex++)
        {
            var item = row.Items[slotIndex];
            if (item.RowId == 0)
                continue;

            var isItemCollected = isSetCollected || (hasSetItem && new BitArray((byte*)glamourDresserItemSetUnlockBits.GetPointer(glamourDresserIndex), row.Items.Count).TryGet(slotIndex, out var slotLocked) && !slotLocked);
            var isItemInInventory = false;
            unsafe
            {
                for (var invIdx = 0; invIdx < 4; invIdx++)
                {
                    var container = InventoryManager.Instance()->GetInventoryContainer((InventoryType)invIdx);
                    for (var slotIdx = 0; slotIdx < container->GetSize(); slotIdx++)
                    {
                        var slot = container->GetInventorySlot(slotIdx);
                        isItemInInventory |= slot->GetBaseItemId() == item.RowId;
                        if (isItemInInventory) break;
                    }
                    if (isItemInInventory) break;
                }
            }

            ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
            var afterIconPos = ImGui.GetCursorPos();
            ImGui.SameLine(0, 0);
            ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
            _textureProvider.DrawIcon(
                (uint)item.Value.Icon,
                new(IconSize * ImGuiHelpers.GlobalScale)
                {
                    TintColor = isSetCollected || isItemCollected || isItemInInventory
                        ? Color.White
                        : ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu")
                            ? Color.White : Color.Grey3
                }
            );

            if (ImGui.IsItemClicked())
                AgentTryon.TryOn(0, item.RowId);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                _unlocksTabUtils.DrawItemTooltip(item.Value,
                    descriptionOverride: isItemCollected
                        ? "In Glamour Dresser"
                        : isItemInInventory
                            ? "In Inventory"
                            : null);
            }

            _imGuiContextMenuService.Draw($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu", builder =>
            {
                builder.AddRestoreItem(item);
                builder.AddViewOutfitGlamourReadyItems(item);
                builder.AddTryOn(item);
                builder.AddItemFinder(item);
                builder.AddCopyItemName(item);
                builder.AddItemSearch(item);
                builder.AddSearchCraftingMethod(item);
                builder.AddOpenOnGarlandTools("item", item.RowId);
            });

            if (isItemCollected || isItemInInventory)
            {
                ImGui.SameLine(0, 0);
                var dotSize = IconSize / 5f * ImGuiHelpers.GlobalScale;
                ImGui.GetWindowDrawList().AddCircleFilled(
                    ImGui.GetCursorScreenPos() + new Vector2(-dotSize, dotSize), dotSize / 2f,
                    isItemCollected
                        ? Color.Yellow.ToUInt()
                        : isItemInInventory
                            ? Color.Green.ToUInt()
                            : Color.Transparent.ToUInt());
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
