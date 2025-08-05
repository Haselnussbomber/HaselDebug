using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Fish.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<FishParameter>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly ImGuiContextMenuService _imGuiContextMenuService;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(FishParameter row)
        => _textService.GetItemName(row.Item.RowId).ExtractText().StripSoftHyphen();

    public override unsafe void DrawColumn(FishParameter row)
    {
        var isItem = row.Item.TryGetValue(out Item item);
        var isEventItem = row.Item.TryGetValue(out EventItem eventItem);
        _debugRenderer.DrawIcon(isItem ? item.Icon : isEventItem ? eventItem.Icon : 0u);

        if (ImGui.Selectable(ToName(row)) && AgentLobby.Instance()->IsLoggedIn)
            AgentFishGuide.Instance()->OpenForItemId(row.Item.RowId, false);

        if (ImGui.IsItemHovered())
            _unlocksTabUtils.DrawItemTooltip(row.Item);

        _imGuiContextMenuService.Draw($"###FishItem_{row.RowId}_ItemContextMenu", builder =>
        {
            if (isItem)
            {
                builder.AddItemFinder(item.RowId);
                builder.AddCopyItemName(item.RowId);
                builder.AddItemSearch(item.RowId);
                builder.AddOpenOnGarlandTools("item", item.RowId);
            }
            else if (isEventItem)
            {
                builder.AddCopyItemName(eventItem.RowId);
            }
        });
    }
}
