using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton, AutoConstruct]
public partial class SetColumn : ColumnString<CustomMirageStoreSetItem>
{
    private const float IconSize = OutfitsTable.IconSize;

    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;
    private readonly ImGuiContextMenuService _imGuiContextMenuService;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(300);
        Flags |= ImGuiTableColumnFlags.DefaultSort;
    }

    public override string ToName(CustomMirageStoreSetItem row)
        => _textService.GetItemName(row.RowId).ToString();

    public override unsafe void DrawColumn(CustomMirageStoreSetItem row)
    {
        var itemFinderModule = ItemFinderModule.Instance();
        var glamourDresserItemIds = itemFinderModule->GlamourDresserItemIds;
        var glamourDresserItemSetUnlockBits = itemFinderModule->GlamourDresserItemSetUnlockBits;
        var glamourDresserIndex = glamourDresserItemIds.IndexOf(row.RowId);
        var hasSetItem = glamourDresserIndex != -1;

        ImGui.BeginGroup();
        ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
        ImGui.SameLine(0, 0);
        ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
        _textureProvider.DrawIcon(
            (uint)row.Set.Value.Icon,
            new(IconSize * ImGuiHelpers.GlobalScale)
            {
                TintColor = hasSetItem
                    ? Color.White
                    : ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###Set_{row.RowId}_Icon_ItemContextMenu")
                        ? Color.White : Color.Grey3
            }
        );

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            if (_textureProvider.TryGetFromGameIcon(new(row.Set.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
            {
                ImGui.Image(textureWrap.Handle, new(textureWrap.Width, textureWrap.Height));
                ImGui.SameLine();
                ImGuiUtils.PushCursorY(textureWrap.Height / 2f - ImGui.GetTextLineHeight() / 2f);
            }
            ImGui.Text(ToName(row));
        }

        var isSetCollected = hasSetItem;

        if (hasSetItem)
        {
            var unlockBitArray = new BitArray((byte*)glamourDresserItemSetUnlockBits.GetPointer(glamourDresserIndex), row.Items.Count);

            for (var slotIndex = 0; slotIndex < row.Items.Count; slotIndex++)
            {
                var slotItem = row.Items[slotIndex];
                if (slotItem.RowId == 0)
                    continue;

                isSetCollected &= unlockBitArray.TryGet(slotIndex, out var slotLocked) && !slotLocked;
            }
        }

        if (isSetCollected)
            OutfitsTable.DrawCollectedCheckmark(_textureProvider);

        ImGui.SameLine();
        ImGui.Selectable($"###SetName_{row.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, IconSize * ImGuiHelpers.GlobalScale));

        ImGui.EndGroup();

        // TODO: preview whole set??
        _imGuiContextMenuService.Draw($"###Set_{row.RowId}_ItemContextMenu", builder =>
        {
            builder.AddTryOn(row.Set.RowId);
            builder.AddItemFinder(row.Set.RowId);
            builder.AddCopyItemName(row.Set.RowId);
            builder.AddItemSearch(row.Set.RowId);
            builder.AddOpenOnGarlandTools("item", row.Set.RowId);
        });

        ImGui.SameLine(IconSize * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X, 0);
        ImGuiUtils.PushCursorY(IconSize * ImGuiHelpers.GlobalScale / 2f - ImGui.GetTextLineHeight() / 2f);
        ImGui.Text(_textService.GetItemName(row.RowId).ToString());
    }
}
