using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton, AutoConstruct]
public partial class ItemsColumn : ColumnString<MirageStoreSetItem>
{
    private const float IconSize = OutfitsTable.IconSize;

    private readonly ITextureProvider _textureProvider;
    private readonly MirageService _mirageService;
    private readonly CabinetService _cabinetService;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    private readonly StringBuilder _stringBuilder = new();

    [AutoPostConstruct]
    public void Initialize()
    {
        Flags |= ImGuiTableColumnFlags.NoSort;
    }

    public override string ToName(MirageStoreSetItem row)
    {
        _stringBuilder.Clear();

        for (var i = 1; i < row.Items.Count; i++)
            _stringBuilder.AppendLine(_textService.GetItemName(row.Items[i].RowId).ToString());

        return _stringBuilder.ToString();
    }

    public override unsafe void DrawColumn(MirageStoreSetItem row)
    {
        var isFullSetCollected = _mirageService.IsFullSetCollected(row.RowId);
        ref var cabinet = ref UIState.Instance()->Cabinet;

        for (var slotIndex = 0; slotIndex < row.Items.Count; slotIndex++)
        {
            var item = row.Items[slotIndex];
            if (item.RowId == 0)
                continue;

            var isItemInInventory = OutfitsTable.IsItemInInventory(item);
            var isItemInDresser = _mirageService.IsItemCollected(item);
            var isItemCollectedInPartialSet = _mirageService.IsSetSlotCollected(row.RowId, slotIndex);
            var isCabinetSupported = _cabinetService.TryGetCabinetId(item, out _);
            var isItemInCabinet = isCabinetSupported && _cabinetService.IsItemCollected(item);

            var isItemCollected = isFullSetCollected || isItemCollectedInPartialSet || isItemInCabinet || isItemInDresser || isItemInInventory;

            ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
            var afterIconPos = ImCursor.Position;
            ImGui.SameLine(0, 0);
            ImCursor.X -= IconSize * ImStyle.Scale;
            _textureProvider.DrawIcon(
                (uint)item.Value.Icon,
                new(IconSize * ImStyle.Scale)
                {
                    TintColor = isItemCollected
                        ? Color.White
                        : ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu")
                            ? Color.White : (Color.White with { A = 0.333f })
                }
            );

            if (ImGui.IsItemClicked())
                AgentTryon.TryOn(0, item.RowId);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                _unlocksTabUtils.DrawItemTooltip(item.Value,
                    description: true switch
                    {
                        _ when isFullSetCollected => _textService.GetAddonText(15643), // Used as part of an outfit glamour.
                        _ when isItemCollectedInPartialSet => _textService.GetAddonText(15636), // Outfit Glamour-ready Item
                        _ when isItemInDresser => "In Glamour Dresser",
                        _ when isItemInCabinet => "In Armoire",
                        _ when isItemInInventory => "In Inventory",
                        _ => "",
                    });
            }

            ImGuiContextMenu.Draw($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu", builder =>
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

            if (!isFullSetCollected && isItemCollected)
            {
                ImGui.SameLine(0, 0);
                var dotSize = IconSize / 5f * ImStyle.Scale;
                ImGui.GetWindowDrawList().AddCircleFilled(
                    ImCursor.ScreenPosition + new Vector2(-dotSize, dotSize), dotSize / 2f,
                    true switch
                    {
                        _ when isItemCollectedInPartialSet => Color.Green.ToUInt(), // Outfit Glamour-ready Item
                        _ when isItemInDresser => Color.Orange.ToUInt(), // In Glamour Dresser
                        _ when isCabinetSupported && !isItemInCabinet => Color.Orange.ToUInt(), // In Inventory
                        _ when isItemInInventory => Color.Yellow.ToUInt(), // In Inventory
                        _ => Color.Transparent.ToUInt(),
                    });
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
