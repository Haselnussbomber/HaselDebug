using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton, AutoConstruct]
public partial class OutfitsTable : Table<MirageStoreSetItem>, IDisposable
{
    public const float IconSize = 32;

    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly SetColumn _setColumn;
    private readonly ItemsColumn _itemsColumn;
    private readonly IClientState _clientState;
    private readonly ItemService _itemService;
    private readonly MirageService _mirageService;
    private readonly CabinetService _cabinetService;
    public bool ArmoireOnly;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<MirageStoreSetItem>.Create(_serviceProvider),
            _setColumn,
            _itemsColumn,
        ];

        _setColumn.Table = this;

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
        return IconSize * ImStyle.Scale + ImStyle.ItemSpacing.Y; // I honestly don't know why using ItemSpacing here works
    }

    public override void LoadRows()
    {
        Rows.Clear();

        foreach (var row in _excelService.GetSheet<MirageStoreSetItem>())
        {
            // is valid set item
            if (row.RowId == 0 || !row.Set.IsValid)
                continue;

            // has items
            if (row.Items.All(i => i.RowId == 0))
                continue;

            // does not only consist of items that can't be worn
            if (row.Items.Where(i => i.RowId != 0).All(i => !_itemService.CanTryOn(i.Value.RowId)))
                continue;

            // apply Cabinet filter, if ticked
            if (ArmoireOnly && !row.Items.Any(item => _cabinetService.TryGetCabinetId(item, out _)))
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
        ImCursor.X -= IconSize * ImStyle.Scale;
        if (textureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var tex, out _))
        {
            var pos = ImCursor.ScreenPosition + ImGuiHelpers.ScaledVector2(IconSize / 2.5f + 4);
            ImGui.GetWindowDrawList().AddImage(tex.Handle, pos, pos + ImGuiHelpers.ScaledVector2(IconSize) / 1.5f, new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
        }
    }

    public bool IsSetCollected(MirageStoreSetItem row)
    {
        var isFullSetCollected = _mirageService.IsFullSetCollected(row.RowId);

        var isFullCabinetSet = row.Items
            .Where(item => item.RowId != 0 && item.IsValid)
            .All(item => _cabinetService.TryGetCabinetId(item, out _));

        var isFullCabinetSetCollected = isFullCabinetSet && row.Items
            .Where(item => item.RowId != 0 && item.IsValid)
            .All(item => _cabinetService.IsItemCollected(item));

        return isFullSetCollected || isFullCabinetSetCollected;
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
