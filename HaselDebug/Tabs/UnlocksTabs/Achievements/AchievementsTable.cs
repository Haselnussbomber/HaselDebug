using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Achievements.Columns;
using AchievementSheet = Lumina.Excel.Sheets.Achievement;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AchievementsTable : Table<AchievementEntry>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly CategoryColumn _categoryColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            EntryRowIdColumn<AchievementEntry, AchievementSheet>.Create(_serviceProvider),
            _unlockedColumn,
            _categoryColumn,
            _nameColumn
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
