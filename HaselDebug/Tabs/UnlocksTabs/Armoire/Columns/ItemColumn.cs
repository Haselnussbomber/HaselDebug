using HaselCommon.Gui.ImGuiTable;
using CabinetSheet = Lumina.Excel.Sheets.Cabinet;

namespace HaselDebug.Tabs.UnlocksTabs.Armoire.Columns;

[RegisterSingleton, AutoConstruct]
public partial class ItemColumn : ColumnString<CabinetSheet>
{
    private const float IconSize = ArmoireTable.IconSize;

    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetStretchWidth(3);
        Flags |= ImGuiTableColumnFlags.DefaultSort;
    }

    public override string ToName(CabinetSheet row)
        => _textService.GetItemName(row.Item.RowId).ToString();

    public override void DrawColumn(CabinetSheet row)
    {
        using (ImRaii.Group())
        {
            ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
            ImGui.SameLine(0, 0);
            ImCursor.X -= IconSize * ImStyle.Scale;
            _textureProvider.DrawIcon(
                (uint)row.Item.Value.Icon,
                new(IconSize * ImStyle.Scale)
            );

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (_textureProvider.TryGetFromGameIcon(new(row.Item.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
                {
                    ImGui.Image(textureWrap.Handle, new(textureWrap.Width, textureWrap.Height));
                    ImGui.SameLine();
                    ImCursor.Y += textureWrap.Height / 2f - ImStyle.TextLineHeight / 2f;
                }
                ImGui.Text(ToName(row));
            }

            if (ArmoireTable.IsItemInCabinet(row))
                ArmoireTable.DrawCollectedCheckmark(_textureProvider);

            ImGui.SameLine();
            ImGui.Selectable($"###Name_{row.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImStyle.ContentRegionAvail.X, IconSize * ImStyle.Scale));
        }

        // TODO: preview whole set??
        ImGuiContextMenu.Draw($"###Item_{row.RowId}_ItemContextMenu", builder =>
        {
            builder.AddTryOn(row.Item.RowId);
            builder.AddItemFinder(row.Item.RowId);
            builder.AddCopyItemName(row.Item.RowId);
            builder.AddItemSearch(row.Item.RowId);
            builder.AddOpenOnGarlandTools("item", row.Item.RowId);
        });

        ImGui.SameLine(IconSize * ImStyle.Scale + ImStyle.ItemSpacing.X, 0);
        ImCursor.Y += IconSize * ImStyle.Scale / 2f - ImStyle.TextLineHeight / 2f;
        ImGui.Text(ToName(row).ToString());
    }
}
