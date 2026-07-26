using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Armoire.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Armoire;

using CabinetSheet = Lumina.Excel.Sheets.Cabinet;

[RegisterSingleton, AutoConstruct]
public partial class ArmoireTable : Table<CabinetSheet>, IDisposable
{
    public const float IconSize = 32;

    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly CategoryColumn _categoryColumn;
    private readonly SubCategoryColumn _subCategoryColumn;
    private readonly ItemColumn _itemColumn;
    private readonly IClientState _clientState;
    private readonly ItemService _itemService;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<CabinetSheet>.Create(_serviceProvider),
            _categoryColumn,
            _subCategoryColumn,
            _itemColumn,
        ];

        Flags |= ImGuiTableFlags.SortTristate;
        Flags |= ImGuiTableFlags.Resizable;

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
        Rows = _excelService.GetSheet<CabinetSheet>().Where(row => row.Order != 0 && row.Item.IsValid && !row.Item.Value.Name.IsEmpty).ToList();
    }

    public override void SortTristate()
    {
        Rows.Sort((a, b) => a.RowId.CompareTo(b.RowId));
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

    public static unsafe bool IsItemInCabinet(CabinetSheet row)
    {
        ref var cabinet = ref UIState.Instance()->Cabinet;
        return cabinet.IsCabinetLoaded() && cabinet.IsItemInCabinet(row.RowId);
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
