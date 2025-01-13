using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;
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
}
