using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Sheets;
using HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton, AutoConstruct]
public partial class OutfitsTable : Table<CustomMirageStoreSetItem>, IDisposable
{
    public const float IconSize = 32;

    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly SetColumn _setColumn;
    private readonly ItemsColumn _itemsColumn;
    private readonly IClientState _clientState;
    private readonly ItemService _itemService;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<CustomMirageStoreSetItem>.Create(_serviceProvider),
            _setColumn,
            _itemsColumn,
        ];

        Flags |= ImGuiTableFlags.SortTristate;

        _clientState.Login += OnLogin;
    }

    public override void Dispose()
    {
        _clientState.Login -= OnLogin;
        base.Dispose();
    }

    private void OnLogin()
    {
        Rows.Clear();
        RowsLoaded = false;
        IsFilterDirty = true;
    }

    public override float CalculateLineHeight()
    {
        return IconSize * ImGuiHelpers.GlobalScaleSafe + ImGui.GetStyle().ItemSpacing.Y; // I honestly don't know why using ItemSpacing here works
    }

    public override unsafe void LoadRows()
    {
        var agent = AgentTryon.Instance();
        var cabinetSheet = _excelService.GetSheet<Cabinet>().Select(row => row.Item.RowId).ToArray();
        foreach (var row in _excelService.GetSheet<CustomMirageStoreSetItem>())
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
            if (row.Items.Where(i => i.RowId != 0).All(i => !_itemService.CanTryOn(i.Value.RowId)))
                continue;

            Rows.Add(row);
        }
    }

    public override void SortTristate()
    {
        Rows.Sort((a, b) => a.Set.RowId.CompareTo(b.Set.RowId));
    }

    public static void DrawCollectedCheckmark(ITextureProvider textureProvider)
    {
        ImGui.SameLine(0, 0);
        ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
        if (textureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var tex, out _))
        {
            var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + ImGuiHelpers.ScaledVector2(IconSize / 2.5f + 4);
            ImGui.GetWindowDrawList().AddImage(tex.Handle, pos, pos + ImGuiHelpers.ScaledVector2(IconSize) / 1.5f, new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
        }
    }

    public static unsafe bool TryGetSetItemBitArray(CustomMirageStoreSetItem row, out BitArray bitArray)
    {
        var mirageManager = MirageManager.Instance();
        if (mirageManager->PrismBoxLoaded)
        {
            var prismBoxItemIndex = mirageManager->PrismBoxItemIds.IndexOf(row.RowId);
            if (prismBoxItemIndex == -1)
            {
                bitArray = default;
                return false;
            }
            bitArray = new BitArray(mirageManager->PrismBoxStain0Ids.GetPointer(prismBoxItemIndex), row.Items.Count);
            return true;
        }

        var itemFinderModule = ItemFinderModule.Instance();
        var glamourDresserIndex = itemFinderModule->GlamourDresserItemIds.IndexOf(row.RowId);
        if (glamourDresserIndex == -1)
        {
            bitArray = default;
            return false;
        }
        bitArray = new BitArray((byte*)itemFinderModule->GlamourDresserItemSetUnlockBits.GetPointer(glamourDresserIndex), row.Items.Count);
        return true;
    }

    public static unsafe bool IsItemInDresser(ItemHandle item)
    {
        var mirageManager = MirageManager.Instance();
        var items = mirageManager->PrismBoxLoaded
            ? mirageManager->PrismBoxItemIds
            : ItemFinderModule.Instance()->GlamourDresserItemIds;
        return items.Contains(item.BaseItemId) || items.Contains(item.BaseItemId + (uint)ItemKind.Hq);
    }

    public static unsafe bool IsItemInInventory(ItemHandle item)
    {
        var isItemInInventory = false;
        for (var invIdx = 0; invIdx < 4; invIdx++)
        {
            var container = InventoryManager.Instance()->GetInventoryContainer((InventoryType)invIdx);
            for (var slotIdx = 0; slotIdx < container->GetSize(); slotIdx++)
            {
                var slot = container->GetInventorySlot(slotIdx);

                isItemInInventory |= slot->GetBaseItemId() == item.BaseItemId;

                if (isItemInInventory)
                    break;
            }

            if (isItemInInventory)
                break;
        }
        return isItemInInventory;
    }
}
