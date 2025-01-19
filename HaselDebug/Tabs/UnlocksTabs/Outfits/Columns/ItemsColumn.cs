using System.Numerics;
using System.Text;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Sheets;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton]
public class ItemsColumn(
    TextService textService,
    TextureService textureService,
    ImGuiContextMenuService imGuiContextMenuService,
    ExcelService excelService,
    UnlocksTabUtils unlocksTabUtils) : ColumnString<CustomMirageStoreSetItem>
{
    private const float IconSize = OutfitsTable.IconSize;
    private readonly StringBuilder _stringBuilder = new();

    public override string ToName(CustomMirageStoreSetItem row)
    {
        _stringBuilder.Clear();

        for (var i = 1; i < row.Items.Count; i++)
            _stringBuilder.AppendLine(textService.GetItemName(row.Items[i].RowId));

        return _stringBuilder.ToString();
    }

    public override unsafe void DrawColumn(CustomMirageStoreSetItem row)
    {
        var glamourDresserItemIds = ItemFinderModule.Instance()->GlamourDresserItemIds;
        var isSetCollected = glamourDresserItemIds.Contains(row.RowId);

        for (var i = 1; i < row.Items.Count; i++)
        {
            var item = row.Items[i];
            if (item.RowId == 0)
                continue;

            var isItemCollected = glamourDresserItemIds.Contains(item.RowId) || glamourDresserItemIds.Contains(item.RowId + 1_000_000);
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
            textureService.DrawIcon(
                item.Value.Icon,
                false,
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
                unlocksTabUtils.DrawItemTooltip(item.Value,
                    descriptionOverride: isItemCollected
                        ? "In Glamour Dresser"
                        : isItemInInventory
                            ? "In Inventory"
                            : null);
            }

            imGuiContextMenuService.Draw($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu", builder =>
            {
                builder.AddRestoreItem(textService, item.RowId);
                builder.AddViewOutfitGlamourReadyItems(textService, excelService, item.RowId);
                builder.AddTryOn(item);
                builder.AddItemFinder(item.RowId);
                builder.AddCopyItemName(item.RowId);
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
                        ? (uint)Color.Yellow
                        : isItemInInventory
                            ? (uint)Color.Green
                            : (uint)Color.Transparent);
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
