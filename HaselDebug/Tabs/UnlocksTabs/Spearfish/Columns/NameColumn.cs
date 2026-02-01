using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Spearfish.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<SpearfishingItem>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(SpearfishingItem row)
        => _textService.GetItemName(row.Item.RowId).ToString();

    public override unsafe void DrawColumn(SpearfishingItem row)
    {
        var item = row.Item.Value;

        _debugRenderer.DrawIcon(item.Icon);

        if (ImGui.Selectable(ToName(row)) && AgentLobby.Instance()->IsLoggedIn)
            AgentFishGuide.Instance()->OpenForItemId(row.Item.RowId, true);

        if (ImGui.IsItemHovered())
            _unlocksTabUtils.DrawItemTooltip(item);

        ImGuiContextMenu.Draw($"###SpearfishItem_{row.RowId}_ItemContextMenu", builder =>
        {
            builder.AddItemFinder(item.RowId);
            builder.AddCopyItemName(item.RowId);
            builder.AddItemSearch(item.RowId);
            builder.AddOpenOnGarlandTools("item", item.RowId);
        });
    }
}
