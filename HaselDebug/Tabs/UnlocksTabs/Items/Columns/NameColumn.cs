using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Items.Columns;

[RegisterTransient, AutoConstruct]
public partial class ItemColumn : ColumnString<Item>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(Item row)
        => _textService.GetItemName(row.RowId).ToString();

    public override unsafe void DrawColumn(Item row)
    {
        _debugRenderer.DrawIcon(row.Icon);

        ImGui.Selectable(ToName(row));

        if (ImGui.IsItemHovered())
            _unlocksTabUtils.DrawItemTooltip(row);

        ImGuiContextMenu.Draw($"###Item_{row.RowId}_ItemContextMenu", builder =>
        {
            builder.AddItemFinder(row.RowId);
            builder.AddCopyItemName(row.RowId);
            builder.AddItemSearch(row.RowId);
            builder.AddOpenOnGarlandTools("item", row.RowId);
        });
    }
}
