using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Quests.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<Quest>
{
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ITextureProvider _textureProvider;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly ImGuiContextMenuService _imGuiContextMenu;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(Quest row)
        => _textService.GetQuestName(row.RowId);

    public override void DrawColumn(Quest row)
    {
        var eventIconType = row.EventIconType.IsValid
            ? row.EventIconType.Value
            : _excelService.GetSheet<EventIconType>().GetRow(1);

        var iconOffset = 1u;
        if (QuestManager.IsQuestComplete(row.RowId))
            iconOffset = 5u;
        else if (row.IsRepeatable)
            iconOffset = 2u;

        if (eventIconType.MapIconAvailable != 0 &&
            _textureProvider.TryGetFromGameIcon(eventIconType.MapIconAvailable + iconOffset, out var tex) &&
            tex.TryGetWrap(out var icon, out _))
        {
            ImGui.Image(icon.Handle, ImGuiHelpers.ScaledVector2(ImGui.GetTextLineHeight()));
        }
        else
        {
            ImGui.Dummy(ImGuiHelpers.ScaledVector2(ImGui.GetTextLineHeight()));
        }

        ImGui.SameLine();

        if (ImGui.Selectable(ToName(row)))
        {
            unsafe
            {
                if (QuestManager.IsQuestComplete(row.RowId) || QuestManager.Instance()->IsQuestAccepted(row.RowId))
                {
                    AgentQuestJournal.Instance()->OpenForQuest(row.RowId, 1);
                }
            }
        }

        _imGuiContextMenu.Draw($"Quest{row.RowId}ContextMenu", builder =>
        {
            builder.AddCopyName(ToName(row));
            builder.AddOpenOnGarlandTools("quest", row.RowId);
        });

        var iconId = row.Icon;
        var currentQuest = row;
        while (iconId == 0 && currentQuest.PreviousQuest[0].RowId != 0)
        {
            currentQuest = currentQuest.PreviousQuest[0].Value;
            iconId = currentQuest.Icon;
        }

        if (iconId != 0 && _textureProvider.TryGetFromGameIcon(iconId, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
        {
            // cool, image preloaded! now the tooltips don't flicker...
        }

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawQuestTooltip(row);
        }
    }
}
