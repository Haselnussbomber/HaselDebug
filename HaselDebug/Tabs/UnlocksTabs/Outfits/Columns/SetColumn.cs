using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton, AutoConstruct]
public partial class SetColumn : ColumnString<MirageStoreSetItem>
{
    private const float IconSize = OutfitsTable.IconSize;

    private readonly TextService _textService;
    private readonly MirageService _mirageService;
    private readonly CabinetService _cabinetService;
    private readonly ITextureProvider _textureProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(300);
        Flags |= ImGuiTableColumnFlags.DefaultSort;
    }

    public override string ToName(MirageStoreSetItem row)
        => _textService.GetItemName(row.RowId).ToString();

    public override void DrawColumn(MirageStoreSetItem row)
    {
        var isFullSetCollected = _mirageService.IsFullSetCollected(row.RowId);

        var isFullCabinetSet = row.Items
            .Where(item => item.RowId != 0 && item.IsValid)
            .All(item => _cabinetService.TryGetCabinetId(item, out _));

        var isFullCabinetSetCollected = isFullCabinetSet && row.Items
            .Where(item => item.RowId != 0 && item.IsValid)
            .All(item => _cabinetService.IsItemCollected(item));

        var isSetCollected = isFullSetCollected || isFullCabinetSetCollected;

        ImGui.BeginGroup();
        ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
        ImGui.SameLine(0, 0);
        ImCursor.X -= IconSize * ImStyle.Scale;
        _textureProvider.DrawIcon(
            (uint)row.Set.Value.Icon,
            new(IconSize * ImStyle.Scale)
            {
                TintColor = isSetCollected
                    ? Color.White
                    : ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###Set_{row.RowId}_Icon_ItemContextMenu")
                        ? Color.White : Color.Text600
            }
        );

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            if (_textureProvider.TryGetFromGameIcon(new(row.Set.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
            {
                ImGui.Image(textureWrap.Handle, new(textureWrap.Width, textureWrap.Height));
                ImGui.SameLine();
                ImCursor.Y += textureWrap.Height / 2f - ImStyle.TextLineHeight / 2f;
            }
            ImGui.Text(ToName(row));
        }

        if (isSetCollected)
            OutfitsTable.DrawCollectedCheckmark(_textureProvider);

        ImGui.SameLine();
        ImGui.Selectable($"###SetName_{row.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImStyle.ContentRegionAvail.X, IconSize * ImStyle.Scale));

        ImGui.EndGroup();

        // TODO: preview whole set??
        ImGuiContextMenu.Draw($"###Set_{row.RowId}_ItemContextMenu", builder =>
        {
            builder.AddTryOn(row.Set.RowId);
            builder.AddItemFinder(row.Set.RowId);
            builder.AddCopyItemName(row.Set.RowId);
            builder.AddItemSearch(row.Set.RowId);
            builder.AddOpenOnGarlandTools("item", row.Set.RowId);
        });

        ImGui.SameLine(IconSize * ImStyle.Scale + ImStyle.ItemSpacing.X, 0);
        ImCursor.Y += IconSize * ImStyle.Scale / 2f - ImStyle.TextLineHeight / 2f;
        ImGui.Text(_textService.GetItemName(row.RowId).ToString());
    }
}
