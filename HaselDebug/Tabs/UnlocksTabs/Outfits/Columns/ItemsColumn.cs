using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Sheets;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ItemsColumn : ColumnString<CustomMirageStoreSetItem>
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
        var isSetInGlamourDresser = OutfitsTable.TryGetSetItemBitArray(row, out var bitArray);
        var isFullSetCollected = isSetInGlamourDresser && row.Items
            .Index()
            .Where((kv) => kv.Item.RowId != 0)
            .All((kv) => bitArray.TryGet(kv.Index, out var slotLocked) && !slotLocked);

        for (var slotIndex = 0; slotIndex < row.Items.Count; slotIndex++)
        {
            var item = row.Items[slotIndex];
            if (item.RowId == 0)
                continue;

            var isItemInInventory = OutfitsTable.IsItemInInventory(item);
            var isItemInDresser = OutfitsTable.IsItemInDresser(item);
            var isItemCollectedInPartialSet = bitArray.TryGet(slotIndex, out var slotLocked) && !slotLocked;
            var isItemCollected = isFullSetCollected || isItemCollectedInPartialSet;

            ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
            var afterIconPos = ImGui.GetCursorPos();
            ImGui.SameLine(0, 0);
            ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
            _textureProvider.DrawIcon(
                (uint)item.Value.Icon,
                new(IconSize * ImGuiHelpers.GlobalScale)
                {
                    TintColor = isItemCollected || isItemInDresser || isItemInInventory
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
                    descriptionOverride: true switch
                    {
                        _ when isFullSetCollected => _textService.GetAddonText(15643), // Used as part of an outfit glamour.
                        _ when isItemCollectedInPartialSet => _textService.GetAddonText(15636), // Outfit Glamour-ready Item
                        _ when isItemInDresser => "In Glamour Dresser",
                        _ when isItemInInventory => "In Inventory",
                        _ => "",
                    });
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

            if (!isFullSetCollected && (isItemCollected || isItemInDresser || isItemInInventory))
            {
                ImGui.SameLine(0, 0);
                var dotSize = IconSize / 5f * ImGuiHelpers.GlobalScale;
                ImGui.GetWindowDrawList().AddCircleFilled(
                    ImGui.GetCursorScreenPos() + new Vector2(-dotSize, dotSize), dotSize / 2f,
                    true switch
                    {
                        _ when isItemCollectedInPartialSet => Color.Yellow.ToUInt(), // Outfit Glamour-ready Item
                        _ when isItemInDresser => Color.Orange.ToUInt(), // In Glamour Dresser
                        _ when isItemInInventory => Color.Orange.ToUInt(), // In Inventory
                        _ => Color.Transparent.ToUInt(),
                    });
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
