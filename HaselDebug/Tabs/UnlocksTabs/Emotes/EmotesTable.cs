using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.Emotes.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes;

[RegisterSingleton, AutoConstruct]
public partial class EmotesTable : Table<Emote>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly CanUseColumn _canUseColumn;
    private readonly ItemColumn _itemColumn;
    private readonly ConditionsColumn _conditionsColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Emote>.Create(_serviceProvider),
            _canUseColumn,
            _itemColumn,
            _conditionsColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Emote>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty && row.Order != 0)
            .ToList();
    }

    // edited version of https://discord.com/channels/581875019861328007/653504487352303619/1529741154281848915
    // still not sure about everything. there are some extra permission conditions and so on
    public static EmoteCondition GetEmoteConditions(Emote emote)
    {
        if (emote.RowId is 60 or 61 || emote.EmoteCategory.RowId == 3)
            return AllConditions;

        if (emote.RowId == 90) // global change pose emote (maybe could check for the other hidden "pose" emotes as well
        {
            return EmoteCondition.Standing
                 | EmoteCondition.SittingInChair
                 | EmoteCondition.SittingOnGround
                 | EmoteCondition.HoldingUmbrella
                 | EmoteCondition.WearingFashionAccessory;
        }

        var timelines = emote.ActionTimeline;
        bool HasTimeline(int index) => index < timelines.Count && timelines[index].RowId != 0;

        var conditions = EmoteCondition.None;

        if (HasTimeline(0))
            conditions |= EmoteCondition.Standing;

        // If can use when swimming
        if (HasTimeline(4) && emote.Unknown2)
            conditions |= EmoteCondition.Swimming;

        // If can use when diving
        if (HasTimeline(4) && emote.Unknown3)
            conditions |= EmoteCondition.Diving;

        // Would kinda confirm "HasAnUpperBodyOrAdditiveVariant"
        if (HasTimeline(2))
            conditions |= EmoteCondition.SittingOnGround;

        // Would alsokinda confirm "HasAnUpperBodyOrAdditiveVariant"
        if (HasTimeline(3))
            conditions |= EmoteCondition.SittingInChair;

        // All emotes with it = true can be done when mounted afaik
        if (HasTimeline(4) && emote.Unknown1)
            conditions |= EmoteCondition.Mounted;

        // If has an upper only/mounted AND not "NeedsFreeHands"/not "UnusableWhenHandsBusy" (?) AKA upper body and also works when holding something
        if (emote.Unknown1 && !emote.Unknown5)
            conditions |= EmoteCondition.HoldingUmbrella | EmoteCondition.HoldingTorch;

        // Not "NeedsFreeHands"/not "UnusableWhenHandsBusy" ? AKA works when holding something
        if (!emote.Unknown5)
            conditions |= EmoteCondition.Fishing;

        return conditions;
    }

    public const EmoteCondition AllConditions
        = EmoteCondition.Standing
        | EmoteCondition.Swimming
        | EmoteCondition.Diving
        | EmoteCondition.SittingOnGround
        | EmoteCondition.SittingInChair
        | EmoteCondition.Mounted
        | EmoteCondition.HoldingUmbrella
        | EmoteCondition.HoldingTorch
        | EmoteCondition.WearingFashionAccessory
        | EmoteCondition.Fishing;

    [Flags]
    public enum EmoteCondition
    {
        None = 0,
        Standing = 1,
        Swimming = 2,
        Diving = 4,
        SittingOnGround = 8,
        SittingInChair = 16,
        Mounted = 32,
        HoldingUmbrella = 64,
        HoldingTorch = 128,
        WearingFashionAccessory = 256,
        Fishing = 512,
    }
}
