using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using AchievementSheet = Lumina.Excel.Sheets.Achievement;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements;

[RegisterSingleton]
public unsafe class AchievementsTable : Table<AchievementEntry>
{
    internal readonly ExcelService _excelService;

    public AchievementsTable(
        DebugRenderer debugRenderer,
        ExcelService excelService,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base("AchievementsTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new CategoryColumn() {
                Label = "Category",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 165,
            },
            new NameColumn(debugRenderer, unlocksTabUtils) {
                Label = "Name",
            }
        ];
    }

    public bool HideSpoilers = true;

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<AchievementSheet>()
            .Where(row => row.RowId != 0 && row.AchievementCategory.RowId != 0 && row.AchievementCategory.IsValid && row.AchievementHideCondition.IsValid)
            .Select(row => new AchievementEntry(this, row))
            .Where(entry => entry.CanShowAchievement)
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<AchievementEntry>
    {
        public override string ToName(AchievementEntry entry)
            => entry.Row.RowId.ToString();

        public override int ToValue(AchievementEntry row)
            => (int)row.Row.RowId;
    }

    private class UnlockedColumn : ColumnBool<AchievementEntry>
    {
        public override unsafe bool ToBool(AchievementEntry entry)
            => entry.IsComplete;
    }

    private class CategoryColumn : ColumnString<AchievementEntry>
    {
        public override string ToName(AchievementEntry entry)
            => entry.CategoryName;
    }

    private class NameColumn(DebugRenderer debugRenderer, UnlocksTabUtils unlocksTabUtils) : ColumnString<AchievementEntry>
    {
        public override string ToName(AchievementEntry entry)
            => entry.Name;

        public override unsafe void DrawColumn(AchievementEntry entry)
        {
            debugRenderer.DrawIcon(entry.Row.Icon);

            var canClick = entry.CanShowName && entry.CanShowCategory;
            var clicked = false;
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canClick))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canClick))
                clicked = ImGui.Selectable(entry.Name);

            if (canClick && clicked)
                AgentAchievement.Instance()->OpenById(entry.Row.RowId);

            if (ImGui.IsItemHovered())
            {
                if (canClick)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                unlocksTabUtils.DrawTooltip(entry.Row.Icon, entry.Name, entry.CategoryName, entry.Description);
            }
        }
    }
}
