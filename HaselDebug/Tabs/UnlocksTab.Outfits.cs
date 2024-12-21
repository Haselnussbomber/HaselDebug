using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    private bool PrismBoxBackedUp = false;
    private DateTime PrismBoxLastCheck = DateTime.MinValue;
    private readonly List<uint> PrismBoxItemIds = [];

    private List<CustomMirageStoreSetItem>? ValidSets;

    public void DrawOutfits()
    {
        using var tab = ImRaii.TabItem("Outfits");
        if (!tab) return;

        var playerState = PlayerState.Instance();
        if (playerState->IsLoaded != 1)
        {
            ImGui.TextUnformatted("PlayerState not loaded.");

            // in case of logout
            if (PrismBoxBackedUp)
            {
                ValidSets = null;
                PrismBoxBackedUp = false;
            }

            return;
        }

        var mirageManager = MirageManager.Instance();
        if (!mirageManager->PrismBoxLoaded)
        {
            if (PrismBoxBackedUp)
            {
                using (Color.Yellow.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted("PrismBox not loaded. Using cache.");
            }
            else
            {
                using (Color.Red.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted("PrismBox not loaded.");
            }
        }
        else
        {
            var hasChanges = false;

            if (DateTime.Now - PrismBoxLastCheck > TimeSpan.FromSeconds(2))
            {
                hasChanges = !CollectionsMarshal.AsSpan(PrismBoxItemIds).SequenceEqual(mirageManager->PrismBoxItemIds);
                PrismBoxLastCheck = DateTime.Now;
            }

            if (!PrismBoxBackedUp || hasChanges)
            {
                PrismBoxItemIds.Clear();
                PrismBoxItemIds.AddRange(mirageManager->PrismBoxItemIds);
                PrismBoxBackedUp = true;
            }
        }

        ValidSets ??= GetValidSets();
        var numCollectedSets = 0;

        foreach (var row in ValidSets!)
        {
            if (PrismBoxItemIds.Contains(row.RowId))
                numCollectedSets++;
        }

        ImGui.TextUnformatted($"{numCollectedSets} out of {ValidSets.Count} filtered sets collected");

        using var table = ImRaii.Table("OutfitsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Set", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Items");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        const float iconSize = 32;
        var scale = ImGuiHelpers.GlobalScale;

        foreach (var row in ValidSets!)
        {
            var isSetCollected = PrismBoxItemIds.Contains(row.RowId);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Set

            ImGui.Dummy(new(iconSize));
            ImGui.SameLine(0, 0);
            ImGuiUtils.PushCursorX(-iconSize);
            TextureService.DrawIcon(row.Set.Value.Icon, false, new(iconSize) { TintColor = isSetCollected ? Color.White : (ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###Set_{row.RowId}_Icon_ItemContextMenu") ? Color.White : Color.Grey3) });

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (TextureProvider.TryGetFromGameIcon(new(row.Set.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
                {
                    ImGui.Image(textureWrap.ImGuiHandle, new(textureWrap.Width, textureWrap.Height));
                    ImGui.SameLine();
                    ImGuiUtils.PushCursorY(textureWrap.Height / 2f - ImGui.GetTextLineHeight() / 2f);
                }
                ImGui.TextUnformatted(TextService.GetItemName(row.Set.RowId));
            }
            if (isSetCollected)
            {
                DrawCollectedCheckmark(iconSize, scale);
            }

            ImGuiContextMenuService.Draw($"###Set_{row.RowId}_Icon_ItemContextMenu", builder =>
            {
                builder.AddTryOn(row.Set);
                builder.AddItemFinder(row.Set.RowId);
                builder.AddCopyItemName(row.Set.RowId);
                builder.AddItemSearch(row.Set);
                builder.AddOpenOnGarlandTools("item", row.Set.RowId);
            });

            ImGui.SameLine();
            ImGui.Selectable($"###SetName_{row.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, iconSize));

            // TODO: preview whole set??

            ImGuiContextMenuService.Draw($"###Set_{row.RowId}_Name_ItemContextMenu", builder =>
            {
                builder.AddTryOn(row.Set);
                builder.AddItemFinder(row.Set.RowId);
                builder.AddCopyItemName(row.Set.RowId);
                builder.AddItemSearch(row.Set);
                builder.AddOpenOnGarlandTools("item", row.Set.RowId);
            });

            ImGui.SameLine(iconSize + ImGui.GetStyle().ItemSpacing.X, 0);
            ImGuiUtils.PushCursorY(iconSize / 2f - ImGui.GetTextLineHeight() / 2f);
            ImGui.TextUnformatted(TextService.GetItemName(row.RowId));

            ImGui.TableNextColumn(); // Items
            for (var i = 1; i < row.Items.Count; i++)
            {
                var item = row.Items[i];
                if (item.RowId != 0 && item.IsValid)
                {
                    var isSetItemCollected = PrismBoxItemIds.Contains(item.RowId) || PrismBoxItemIds.Contains(item.RowId + 1_000_000);

                    ImGui.Dummy(new(iconSize));
                    ImGui.SameLine(0, 0);
                    ImGuiUtils.PushCursorX(-iconSize);
                    TextureService.DrawIcon(item.Value.Icon, false, new(iconSize) { TintColor = isSetCollected || isSetItemCollected ? Color.White : (ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu") ? Color.White : Color.Grey3) });

                    if (ImGui.IsItemClicked())
                    {
                        AgentTryon.TryOn(0, item.RowId);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                        using var tooltip = ImRaii.Tooltip();
                        if (TextureProvider.TryGetFromGameIcon(new(item.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
                        {
                            ImGui.Image(textureWrap.ImGuiHandle, textureWrap.Size);
                            ImGui.SameLine();
                            ImGuiUtils.PushCursorY(textureWrap.Height / 2f - ImGui.GetTextLineHeight() / 2f);
                        }
                        ImGui.TextUnformatted(TextService.GetItemName(item.RowId));
                    }

                    ImGuiContextMenuService.Draw($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu", builder =>
                    {
                        builder.AddTryOn(item);
                        builder.AddItemFinder(item.RowId);
                        builder.AddCopyItemName(item.RowId);
                        builder.AddItemSearch(item);
                        builder.AddSearchCraftingMethod(item);
                        builder.AddOpenOnGarlandTools("item", item.RowId);
                    });

                    if (isSetItemCollected)
                    {
                        DrawCollectedCheckmark(iconSize, scale);
                    }

                    ImGui.SameLine();
                }
            }
            ImGui.NewLine();
        }
    }

    private List<CustomMirageStoreSetItem>? GetValidSets()
    {
        var list = new List<CustomMirageStoreSetItem>();
        var cabinetSheet = ExcelService.GetSheet<Cabinet>().Select(row => row.Item.RowId).ToArray();

        foreach (var row in ExcelService.GetSheet<CustomMirageStoreSetItem>())
        {
            // is valid set item
            if (row.RowId == 0 || !row.Set.IsValid)
                continue;

            // has items
            if (row.Items.All(i => i.RowId == 0))
                continue;

            // does not only consist of cabinet items
            if (row.Items.Where(i => i.RowId != 0).All(i => cabinetSheet.Contains(i.RowId)))
                continue;

            // does not only consist of items that can't be worn
            if (row.Items.Where(i => i.RowId != 0).All(i => !ItemService.CanTryOn(i.Value)))
                continue;

            list.Add(row);
        }

        return list;
    }

    private void DrawCollectedCheckmark(float iconSize, float scale)
    {
        ImGui.SameLine(0, 0);
        ImGuiUtils.PushCursorX(-iconSize);
        if (TextureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var tex, out _))
        {
            var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + new Vector2(iconSize / 2.5f + 4 * scale);
            ImGui.GetWindowDrawList().AddImage(tex.ImGuiHandle, pos, pos + new Vector2(iconSize / 1.5f), new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
        }
    }
}

[Sheet("EquipRaceCategory")]
public readonly unsafe struct CustomEquipRaceCategory(ExcelPage page, uint offset, uint row) : IExcelRow<CustomEquipRaceCategory>
{
    public uint RowId => row;

    public readonly Collection<bool> Races => new(page, offset, offset, &RaceCtor, 8);
    public readonly Collection<bool> Sexes => new(page, offset, offset, &SexCtor, 2);

    private static bool RaceCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadBool(offset + i);
    private static bool SexCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadPackedBool(offset + 8, (byte)i);

    static CustomEquipRaceCategory IExcelRow<CustomEquipRaceCategory>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}

[Sheet("MirageStoreSetItem")]
public readonly unsafe struct CustomMirageStoreSetItem(ExcelPage page, uint offset, uint row) : IExcelRow<CustomMirageStoreSetItem>
{
    public uint RowId => row;

    public readonly RowRef<Item> Set => new(page.Module, RowId, page.Language);

    /* based on EquipSlotCategory sheet used in E8 ?? ?? ?? ?? 85 C0 74 56 48 8B 0D
       0: MainHand?
       1: OffHand?
       2: Head
       3: Body
       4: Gloves
       5: Legs
       6: Feet
       7: Ears
       8: Neck
       9: Wrists
       10: Ring
    */
    public readonly Collection<RowRef<Item>> Items => new(page, parentOffset: offset, offset: offset, &ItemCtor, size: 11);

    private static RowRef<Item> ItemCtor(ExcelPage page, uint parentOffset, uint offset, uint i) =>
        new(page.Module, page.ReadUInt32(offset + i * 4), page.Language);

    static CustomMirageStoreSetItem IExcelRow<CustomMirageStoreSetItem>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
