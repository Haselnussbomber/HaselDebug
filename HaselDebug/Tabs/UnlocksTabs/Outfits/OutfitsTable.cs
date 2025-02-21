using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Sheets;
using HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton, AutoConstruct]
public partial class OutfitsTable : Table<CustomMirageStoreSetItem>, IDisposable
{
    public const float IconSize = 32;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly GlobalScaleObserver _globalScaleObserver;
    private readonly SetColumn _setColumn;
    private readonly ItemsColumn _itemsColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<CustomMirageStoreSetItem>.Create(),
            _setColumn,
            _itemsColumn,
        ];

        Flags |= ImGuiTableFlags.SortTristate;

        _globalScaleObserver.ScaleChange += OnScaleChange;
        OnScaleChange(ImGuiHelpers.GlobalScale);
    }

    public new void Dispose()
    {
        _globalScaleObserver.ScaleChange -= OnScaleChange;
        base.Dispose();
    }

    private void OnScaleChange(float scale)
    {
        LineHeight = IconSize * scale + ImGui.GetStyle().ItemSpacing.Y; // I honestly don't know why using ItemSpacing here works
    }

    public override void LoadRows()
    {
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
            if (row.Items.Where(i => i.RowId != 0).All(i => !_itemService.CanTryOn(i.Value)))
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
            ImGui.GetWindowDrawList().AddImage(tex.ImGuiHandle, pos, pos + ImGuiHelpers.ScaledVector2(IconSize) / 1.5f, new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
        }
    }
}
