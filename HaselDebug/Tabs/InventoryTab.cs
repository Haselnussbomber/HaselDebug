using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace HaselDebug.Tabs;

#pragma warning disable SeStringRenderer
public unsafe class InventoryTab(DebugRenderer DebugRenderer, TextService TextService, ExcelService ExcelService, ItemService ItemService) : DebugTab
{
    public override bool DrawInChild => false;

    private InventoryType? selectedInventoryType = InventoryType.Inventory1;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("InventoryTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);

        using var tabs = ImRaii.TabBar("InventoryTabs");
        if (!tabs) return;

        DrawInventoriesTab();
        DrawCurrenciesTab();
    }

    private void DrawInventoriesTab()
    {
        using var tab = ImRaii.TabItem("Inventories");
        if (!tab) return;

        DrawInventoryTypeList();

        if (selectedInventoryType == null)
            return;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        DrawInventoryType((InventoryType)selectedInventoryType);
    }

    private void DrawInventoryTypeList()
    {
        using var table = ImRaii.Table("InventoryTypeTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Type");
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var inventoryManager = InventoryManager.Instance();

        foreach (var inventoryType in Enum.GetValues<InventoryType>())
        {
            var listContainer = inventoryManager->GetInventoryContainer(inventoryType);
            if (listContainer == null) continue;

            using var itemDisabled = ImRaii.Disabled(listContainer->Loaded == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Type
            if (ImGui.Selectable(inventoryType.ToString(), selectedInventoryType == inventoryType, ImGuiSelectableFlags.SpanAllColumns))
            {
                selectedInventoryType = inventoryType;
            }
            using (var contextMenu = ImRaii.ContextPopupItem($"##InventoryContext{inventoryType}"))
            {
                if (contextMenu)
                {
                    if (ImGui.MenuItem("Copy Name"))
                    {
                        ImGui.SetClipboardText(inventoryType.ToString());
                    }

                    if (ImGui.MenuItem("Copy Address"))
                    {
                        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
                        ImGui.SetClipboardText($"0x{(nint)container:X}");
                    }
                }
            }

            ImGui.TableNextColumn(); // Size
            ImGui.TextUnformatted(listContainer->Size.ToString());
        }
    }

    private void DrawInventoryType(InventoryType inventoryType)
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
        using var disabled = ImRaii.Disabled(container->Loaded == 0);

        using var itemTable = ImRaii.Table("InventoryItemTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY);
        if (!itemTable) return;
        ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("ItemId", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Quantity", ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Item");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < container->Size; i++)
        {
            var slot = container->GetInventorySlot(i);
            if (slot == null) continue;

            var itemId = slot->GetItemId();
            var quantity = slot->GetQuantity();

            using var disableditem = ImRaii.Disabled(itemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // ItemId
            ImGui.TextUnformatted(itemId.ToString());

            ImGui.TableNextColumn(); // Quantity
            ImGui.TextUnformatted(quantity.ToString());

            ImGui.TableNextColumn(); // Item
            if (itemId != 0 && quantity != 0)
            {
                var itemName = TextService.GetItemName(itemId);

                if (itemId is > 1_000_000 and < 2_000_000)
                    itemName += " " + SeIconChar.HighQuality.ToIconString();

                var itemNameSeStr = new SeStringBuilder()
                    .PushColorType(ItemService.GetItemRarityColorType(itemId))
                    .Append(itemName)
                    .PopColorType()
                    .ToReadOnlySeString();

                DebugRenderer.DrawIcon(ItemService.GetIconId(itemId), ItemService.IsHighQuality(itemId));
                DebugRenderer.DrawPointerType(slot, typeof(InventoryItem), new NodeOptions()
                {
                    AddressPath = new AddressPath([(nint)inventoryType, slot->Slot]),
                    SeStringTitle = itemNameSeStr
                });
            }
        }
    }

    private void DrawCurrenciesTab()
    {
        using var tab = ImRaii.TabItem("Currencies");
        if (!tab) return;

        var inventoryManager = InventoryManager.Instance();

        ImGui.TextUnformatted($"EmptySlotsInBag: {inventoryManager->GetEmptySlotsInBag():N0}");
        ImGui.TextUnformatted($"Gil: {inventoryManager->GetGil():N0}");
        ImGui.TextUnformatted($"RetainerGil: {inventoryManager->GetRetainerGil():N0}");
        ImGui.TextUnformatted($"GoldSaucerCoin: {inventoryManager->GetGoldSaucerCoin():N0}");
        ImGui.TextUnformatted($"WolfMarks: {inventoryManager->GetWolfMarks():N0}");
        ImGui.TextUnformatted($"AlliedSeals: {inventoryManager->GetAlliedSeals():N0}");
        ImGui.TextUnformatted($"CompanySeals: {inventoryManager->GetCompanySeals(PlayerState.Instance()->GrandCompany):N0}");
        ImGui.TextUnformatted($"MaxCompanySeals: {inventoryManager->GetMaxCompanySeals(PlayerState.Instance()->GrandCompany):N0}");

        foreach (var row in ExcelService.GetSheet<TomestonesItem>()!)
        {
            ImGui.TextUnformatted($"TomestoneItem #{row.RowId} ({row.Item.Value?.Name ?? ""}): {inventoryManager->GetTomestoneCount(row.Item.Row):N0}");
        }
    }
}
