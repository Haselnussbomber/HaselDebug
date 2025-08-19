using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;
using Lumina.Text;
using ItemUtil = Dalamud.Utility.ItemUtil;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InventoryTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private InventoryType? _selectedInventoryType = InventoryType.Inventory1;

    public override bool DrawInChild => false;

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

        if (_selectedInventoryType == null)
            return;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        DrawInventoryType((InventoryType)_selectedInventoryType);
    }

    private void DrawInventoryTypeList()
    {
        using var table = ImRaii.Table("InventoryTypeTable"u8, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Type");
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var inventoryManager = InventoryManager.Instance();

        foreach (var inventoryType in Enum.GetValues<InventoryType>())
        {
            var listContainer = inventoryManager->GetInventoryContainer(inventoryType);
            if (listContainer == null) continue;

            using var itemDisabled = ImRaii.Disabled(listContainer->GetSize() == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Type
            if (ImGui.Selectable(inventoryType.ToString(), _selectedInventoryType == inventoryType, ImGuiSelectableFlags.SpanAllColumns))
            {
                _selectedInventoryType = inventoryType;
            }
            _imGuiContextMenu.Draw($"##InventoryContext{inventoryType}", builder =>
            {
                var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);

                builder.AddCopyName(_textService, inventoryType.ToString());
                builder.AddCopyAddress(_textService, (nint)container);
            });

            ImGui.TableNextColumn(); // Size
            ImGui.Text(listContainer->GetSize().ToString());
        }
    }

    private void DrawInventoryType(InventoryType inventoryType)
    {
        var container = InventoryManager.Instance()->GetInventoryContainer(inventoryType);
        using var disabled = ImRaii.Disabled(container->GetSize() == 0);

        using var itemTable = ImRaii.Table("InventoryItemTable"u8, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!itemTable) return;
        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("ItemId"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Quantity"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Item");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < container->GetSize(); i++)
        {
            var slot = container->GetInventorySlot(i);
            if (slot == null) continue;

            var itemId = slot->GetItemId();
            var quantity = slot->GetQuantity();

            using var disableditem = ImRaii.Disabled(itemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // ItemId
            ImGui.Text(itemId.ToString());

            ImGui.TableNextColumn(); // Quantity
            ImGui.Text(quantity.ToString());

            ImGui.TableNextColumn(); // Item
            if (itemId != 0 && quantity != 0)
            {
                var itemName = _textService.GetItemName(itemId);

                if (ItemUtil.IsHighQuality(itemId))
                    itemName += " " + SeIconChar.HighQuality.ToIconString();

                var itemNameSeStr = new SeStringBuilder()
                    .PushColorType(ItemUtil.GetItemRarityColorType(itemId))
                    .Append(itemName)
                    .PopColorType()
                    .ToReadOnlySeString();

                _debugRenderer.DrawIcon(_itemService.GetIconId(itemId), ItemUtil.IsHighQuality(itemId));
                _debugRenderer.DrawPointerType(slot, inventoryType is InventoryType.Cosmopouch1 or InventoryType.Cosmopouch2 ? typeof(WKSContentInventoryItem) : typeof(InventoryItem), new NodeOptions()
                {
                    AddressPath = new AddressPath([(nint)inventoryType, slot->Slot]),
                    SeStringTitle = itemNameSeStr
                });

                if (itemId is 8575 or 8693 or 8694 or 8695 or 8696 or 8698 or 8699) // IsWeddingRelatedItemId
                {
                    var date =
                        (slot->GetMateriaId(0) << 4 | (slot->GetMateriaGrade(0) & 0xF)) << 16 |
                         slot->GetMateriaId(1) << 4 | (slot->GetMateriaGrade(1) & 0xF);
                    ImGui.Text("Date: " + _seStringEvaluator.EvaluateFromAddon(1551, [date]).ToString());
                }
            }
        }
    }

    private void DrawCurrenciesTab()
    {
        using var tab = ImRaii.TabItem("Currencies");
        if (!tab) return;

        var inventoryManager = InventoryManager.Instance();

        ImGui.Text($"EmptySlotsInBag: {inventoryManager->GetEmptySlotsInBag():N0}");
        ImGui.Text($"Gil: {inventoryManager->GetGil():N0}");
        ImGui.Text($"RetainerGil: {inventoryManager->GetRetainerGil():N0}");
        ImGui.Text($"GoldSaucerCoin: {inventoryManager->GetGoldSaucerCoin():N0}");
        ImGui.Text($"WolfMarks: {inventoryManager->GetWolfMarks():N0}");
        ImGui.Text($"AlliedSeals: {inventoryManager->GetAlliedSeals():N0}");
        ImGui.Text($"CompanySeals: {inventoryManager->GetCompanySeals(PlayerState.Instance()->GrandCompany):N0}");
        ImGui.Text($"MaxCompanySeals: {inventoryManager->GetMaxCompanySeals(PlayerState.Instance()->GrandCompany):N0}");

        foreach (var row in _excelService.GetSheet<TomestonesItem>()!)
        {
            ImGui.Text($"TomestoneItem #{row.RowId} ({row.Item.Value.Name}): {inventoryManager->GetTomestoneCount(row.Item.RowId):N0}");
        }
    }
}
