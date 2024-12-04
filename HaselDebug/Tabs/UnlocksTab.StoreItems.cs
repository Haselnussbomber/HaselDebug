/*
 * LuminaSupplemental not Lumina 5 compatible
 * 
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;
using LuminaSupplemental.Excel.Model;
using Cabinet = Lumina.Excel.Sheets.Cabinet;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    private List<StoreItem> StoreItemsList = [];
    private FrozenDictionary<uint, uint> CabinetItems = null!;

    public void UpdateStoreItems()
    {
        StoreItemsList = CsvLoader.LoadResource<StoreItem>(CsvLoader.StoreItemResourceName, out _, DataManager.GameData, TextService.ClientLanguage.ToLumina())
            .Where(row => row.RowId != 0 && row.Item.RowId is not (0 or 5827) && row.Item.Value != null)
            .GroupBy(row => row.FittingShopItemSetId)
            .SelectMany(group => group)
            .DistinctBy(row => row.Item.Row)
            .OrderBy(row => row.Item.Value!.ItemUICategory.Value?.Name.ExtractText())
            .ThenBy(row => TextService.GetItemName(row.Item.Row))
            .ToList();

        CabinetItems = ExcelService.GetSheet<Cabinet>().DistinctBy(row => row.Item.RowId).ToFrozenDictionary(row => row.Item.Row, row => row.RowId);
    }

    public void DrawStoreItems()
    {
        using var tab = ImRaii.TabItem("Store Items");
        if (!tab) return;

        var cabinet = UIState.Instance()->Cabinet;
        var isCabinetLoaded = cabinet.IsCabinetLoaded();
        var isPrismBoxLoaded = MirageManager.Instance()->PrismBoxLoaded;

        if (!isCabinetLoaded)
        {
            TextService.DrawWrapped(Color.Red, "UnlocksTab.CabinetNotLoaded");
        }

        if (!isPrismBoxLoaded)
        {
            TextService.DrawWrapped(Color.Red, "UnlocksTab.PrismBoxNotLoaded");
        }

        // i really need to make a sortable, searchable table soon
        using var table = ImRaii.Table("StoreItemsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Item Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Item Category", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 200);
        ImGui.TableSetupColumn("Item");
        ImGui.TableSetupColumn("Collected", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 100);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty)
        {
            StoreItemsList.Sort((a, b) => sortSpecs.Specs.ColumnIndex switch
            {
                0 when sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending => (int)(a.ItemId - b.ItemId),
                0 when sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending => (int)(b.ItemId - a.ItemId),
                1 when sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending => (a.Item.Value!.ItemUICategory.Value!.Name.ExtractText() ?? string.Empty).CompareTo(b.Item.Value!.ItemUICategory.Value!.Name.ExtractText() ?? string.Empty),
                1 when sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending => (b.Item.Value!.ItemUICategory.Value!.Name.ExtractText() ?? string.Empty).CompareTo(a.Item.Value!.ItemUICategory.Value!.Name.ExtractText() ?? string.Empty),
                2 when sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending => TextService.GetItemName(a.Item.Row).CompareTo(TextService.GetItemName(b.Item.Row)),
                2 when sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending => TextService.GetItemName(b.Item.Row).CompareTo(TextService.GetItemName(a.Item.Row)),
                _ => 0,
            });

            sortSpecs.SpecsDirty = false;
        }

        var count = StoreItemsList.Count;
        var tribe = PlayerState.Instance()->Tribe;
        var sex = PlayerState.Instance()->Sex;

        var imGuiListClipperPtr = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        imGuiListClipperPtr.Begin(count, ImGui.GetTextLineHeightWithSpacing());
        while (imGuiListClipperPtr.Step())
        {
            for (var i = imGuiListClipperPtr.DisplayStart; i < imGuiListClipperPtr.DisplayEnd; i++)
            {
                if (i >= count)
                    return;

                var row = StoreItemsList[i];

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // ItemId
                DebugRenderer.DrawCopyableText(row.ItemId.ToString());

                ImGui.TableNextColumn(); // Item Category
                DebugRenderer.DrawCopyableText(row.Item.Value!.ItemUICategory.Value!.Name.ExtractText() ?? string.Empty);

                ImGui.TableNextColumn(); // Item
                DrawSelectableItem(row.Item.Value, $"StoreItemsList{i}");

                /* i'd love to link the store, but that information is not available
                if (ImGui.Selectable(TextService.GetItemName(row.ItemId)))
                {
                    Util.OpenLink($"https://store.finalfantasyxiv.com/ffxivstore/product/{row.RowId}");
                }
                * /

                ImGui.TableNextColumn(); // Collected

                if (row.Item.Value.EquipSlotCategory.Row != 0)
                {
                    CabinetItems.TryGetValue(row.Item.Row, out var cabinetItemId);
                    var inCabinet = isCabinetLoaded && cabinetItemId != 0 && UIState.Instance()->Cabinet.IsItemInCabinet((int)cabinetItemId);
                    var inPrismBox = isPrismBoxLoaded && MirageManager.Instance()->PrismBoxItemIds.Contains(row.Item.Row);

                    // Prism Box
                    if (isPrismBoxLoaded)
                    {
                        TextureService.DrawPart("ItemDetail", 5, inPrismBox ? 4u : 1, new(ImGui.GetTextLineHeight()));
                        if (ImGui.IsItemHovered())
                        {
                            using var tooltip = ImRaii.Tooltip();
                            TextService.Draw(inPrismBox ? "UnlocksTab.StoredInPrismBox" : "UnlocksTab.NotStoredInPrismBox");
                        }
                    }

                    // Armoire
                    if (isCabinetLoaded && cabinetItemId != 0)
                    {
                        if (isPrismBoxLoaded)
                            ImGui.SameLine();

                        TextureService.DrawPart("ItemDetail", 5, inCabinet ? 5u : 2, new(ImGui.GetTextLineHeight()));
                        if (ImGui.IsItemHovered())
                        {
                            using var tooltip = ImRaii.Tooltip();
                            TextService.Draw(inCabinet ? "UnlocksTab.StoredInCabinet" : "UnlocksTab.NotStoredInCabinet");
                        }
                    }
                }
                else if (ItemService.IsUnlockable(row.Item.Row))
                {
                    var isUnlocked = ItemService.IsUnlocked(row.Item.Row);
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                        ImGui.TextUnformatted(isUnlocked.ToString());
                }
            }
        }

        imGuiListClipperPtr.End();
        imGuiListClipperPtr.Destroy();
    }
}
*/
