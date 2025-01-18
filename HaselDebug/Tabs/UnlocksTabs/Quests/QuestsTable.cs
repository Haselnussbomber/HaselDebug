using System.Linq;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Quests;

[RegisterSingleton]
public class QuestsTable : Table<Quest>, IDisposable
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;

    public QuestsTable(
        DebugRenderer debugRenderer,
        LanguageProvider languageProvider,
        ExcelService excelService,
        TextService textService,
        ITextureProvider textureProvider,
        UnlocksTabUtils unlocksTabUtils,
        ImGuiContextMenuService imGuiContextMenu) : base("QuestsTable", languageProvider)
    {
        _excelService = excelService;
        _textService = textService;

        Columns = [
            new RowIdColumn(debugRenderer) {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort,
                Width = 60,
            },
            new QuestIdColumn(debugRenderer) {
                Label = "QuestId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new QuestStatusColumn() {
                Label = "Status",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new RepeatableColumn() {
                Label = "Repeatable",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new CategoryColumn(debugRenderer) {
                Label = "Category",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 250,
            },
            new NameColumn(textService, excelService, textureProvider, unlocksTabUtils, imGuiContextMenu) {
                Label = "Name",
            },
        ];

        Flags |= ImGuiTableFlags.Resizable; // TODO: no worky?
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Quest>().Where(row => row.RowId != 0 && !string.IsNullOrEmpty(_textService.GetQuestName(row.RowId))).ToList();
    }

    private class RowIdColumn(DebugRenderer debugRenderer) : ColumnNumber<Quest>
    {
        public override int ToValue(Quest row)
            => (int)row.RowId;

        public override void DrawColumn(Quest row)
            => debugRenderer.DrawCopyableText(ToName(row));
    }

    private class QuestIdColumn(DebugRenderer debugRenderer) : ColumnNumber<Quest>
    {
        public override int ToValue(Quest row)
            => (int)(row.RowId - 0x10000);

        public override void DrawColumn(Quest row)
            => debugRenderer.DrawCopyableText(ToName(row));
    }

    private class RepeatableColumn : ColumnBool<Quest>
    {
        public override unsafe bool ToBool(Quest row)
            => row.IsRepeatable;

        public override unsafe void DrawColumn(Quest row)
            => ImGui.TextUnformatted(Names[ToBool(row) ? 1 : 0]);
    }

    private class CategoryColumn(DebugRenderer debugRenderer) : ColumnString<Quest>
    {
        public override string ToName(Quest row)
        {
            if (row.JournalGenre.RowId == 0 || !row.JournalGenre.IsValid)
                return string.Empty;

            return row.JournalGenre.Value.Name.ExtractText().StripSoftHypen();
        }

        public override void DrawColumn(Quest row)
        {
            if (row.JournalGenre.RowId != 0 && row.JournalGenre.IsValid)
            {
                debugRenderer.DrawIcon((uint)row.JournalGenre.Value.Icon);
                ImGui.TextUnformatted(ToName(row));
            }
        }
    }

    private class NameColumn(
        TextService textService,
        ExcelService excelService,
        ITextureProvider textureProvider,
        UnlocksTabUtils unlocksTabUtils,
        ImGuiContextMenuService imGuiContextMenu) : ColumnString<Quest>
    {
        public override string ToName(Quest row)
            => textService.GetQuestName(row.RowId);

        public override void DrawColumn(Quest row)
        {
            var eventIconType = row.EventIconType.IsValid
                ? row.EventIconType.Value
                : excelService.GetSheet<EventIconType>().GetRow(1);

            var iconOffset = 1u;
            if (QuestManager.IsQuestComplete(row.RowId))
                iconOffset = 5u;
            else if (row.IsRepeatable)
                iconOffset = 2u;

            if (eventIconType.MapIconAvailable != 0 &&
                textureProvider.TryGetFromGameIcon(eventIconType.MapIconAvailable + iconOffset, out var tex) &&
                tex.TryGetWrap(out var icon, out _))
            {
                ImGui.Image(icon.ImGuiHandle, ImGuiHelpers.ScaledVector2(ImGui.GetTextLineHeight()));
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

            imGuiContextMenu.Draw($"Quest{row.RowId}ContextMenu", builder =>
            {
                builder.AddCopyName(textService, ToName(row));
                builder.AddOpenOnGarlandTools("quest", row.RowId);
            });

            if (ImGui.IsItemHovered())
            {
                unlocksTabUtils.DrawQuestTooltip(row);
            }
        }
    }
}

[Flags]
public enum QuestStatus
{
    Incomplete = 1,
    Complete = 2,
    Accepted = 4
}

public class QuestStatusColumn : ColumnFlags<QuestStatus, Quest>
{
    private QuestStatus _filterValue;
    public override QuestStatus FilterValue => _filterValue;

    public QuestStatusColumn()
    {
        AllFlags = Enum.GetValues<QuestStatus>().Aggregate((a, b) => a | b);
        _filterValue = AllFlags;
    }

    public virtual unsafe QuestStatus ToStatus(Quest row)
    {
        if (QuestManager.Instance()->IsQuestAccepted(row.RowId))
            return QuestStatus.Accepted;

        if (QuestManager.IsQuestComplete(row.RowId))
            return QuestStatus.Complete;

        return QuestStatus.Incomplete;
    }

    public override bool ShouldShow(Quest row)
    {
        var value = ToStatus(row);
        return (FilterValue.HasFlag(QuestStatus.Accepted) && value == QuestStatus.Accepted) ||
               (FilterValue.HasFlag(QuestStatus.Complete) && value == QuestStatus.Complete) ||
               (FilterValue.HasFlag(QuestStatus.Incomplete) && value == QuestStatus.Incomplete);
    }

    public override unsafe void DrawColumn(Quest row)
    {
        var value = ToStatus(row);
        using (ImRaii.PushColor(ImGuiCol.Text, (uint)(value == QuestStatus.Complete ? Color.Green : (value == QuestStatus.Accepted ? Color.Yellow : Color.Red))))
            ImGui.TextUnformatted(Enum.GetName(value));
    }

    public override unsafe int Compare(Quest a, Quest b)
        => ToStatus(a).CompareTo(ToStatus(b));

    public override void SetValue(QuestStatus value, bool enable)
    {
        if (enable)
            _filterValue |= value;
        else
            _filterValue &= ~value;
    }
}
