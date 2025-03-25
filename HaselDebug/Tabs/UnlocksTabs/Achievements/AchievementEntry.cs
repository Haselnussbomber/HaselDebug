using Dalamud.Utility;
using Achievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using AchievementSheet = Lumina.Excel.Sheets.Achievement;

namespace HaselDebug.Tabs.UnlocksTabs.Achievements;

public record AchievementEntry : IUnlockEntry
{
    private readonly AchievementsTable _table;

    public uint RowId => Row.RowId;
    public AchievementSheet Row { get; init; }

    public unsafe bool IsComplete
    {
        get
        {
            var achievement = Achievement.Instance();
            return achievement->IsComplete((int)Row.RowId);
        }
    }

    public bool IsHiddenName => Row.AchievementHideCondition.Value.HideName == true;
    public bool IsHiddenCategory => Row.AchievementCategory.Value.HideCategory == true;
    public bool IsHiddenAchievement => Row.AchievementHideCondition.Value.HideAchievement == true;

    public bool CanShow => !_table.HideSpoilers || IsComplete;
    public bool CanShowName => CanShow || !IsHiddenName;
    public bool CanShowCategory => CanShow || !IsHiddenCategory;
    public bool CanShowDescription => CanShow || (!IsHiddenName && !IsHiddenCategory && !IsHiddenAchievement); // idk actually
    public bool CanShowAchievement => CanShow || !IsHiddenAchievement;

    public string Name => CanShowAchievement && CanShowName ? Row.Name.ExtractText().StripSoftHyphen() : "???";
    public string CategoryName => CanShowAchievement && CanShowCategory ? Row.AchievementCategory.Value.Name.ExtractText().StripSoftHyphen() : "???";
    public string Description => CanShowAchievement && CanShowDescription ? Row.Description.ExtractText().StripSoftHyphen() : "???";

    public AchievementEntry(AchievementsTable table, AchievementSheet row)
    {
        _table = table;
        Row = row;
    }
}
