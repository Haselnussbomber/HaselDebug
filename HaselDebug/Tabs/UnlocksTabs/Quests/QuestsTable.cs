using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Quests;

[RegisterSingleton]
public class QuestsTable : Table<Quest>, IDisposable
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;

    public QuestsTable(
        LanguageProvider languageProvider,
        ExcelService excelService,
        TextService textService) : base("QuestsTable", languageProvider)
    {
        _excelService = excelService;
        _textService = textService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort,
                Width = 60,
            },
            new QuestIdColumn() {
                Label = "QuestId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new CompletedColumn() {
                Label = "Completed",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new NameColumn(textService) {
                Label = "Name",
            },
        ];

        /*
        ImGui.TableSetupColumn("Currency Reward", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Emote Reward", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Instance Unlock", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Rewards");
        ImGui.TableSetupColumn("Optional Rewards");
        ImGui.TableSetupColumn("Gil Reward", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);
        */
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Quest>().Where(row => row.RowId != 0 && !string.IsNullOrEmpty(_textService.GetQuestName(row.RowId))).ToList();
    }

    private class RowIdColumn : ColumnNumber<Quest>
    {
        public override int ToValue(Quest row)
            => (int)row.RowId;
    }

    private class QuestIdColumn : ColumnNumber<Quest>
    {
        public override int ToValue(Quest row)
            => (int)(row.RowId - 0x10000);
    }

    private class CompletedColumn : ColumnBool<Quest>
    {
        public override unsafe bool ToBool(Quest row)
            => UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted((ushort)row.RowId + 0x10000u);
    }

    private class NameColumn(TextService textService) : ColumnString<Quest>
    {
        public override string ToName(Quest row)
            => textService.GetQuestName(row.RowId);
    }
}
