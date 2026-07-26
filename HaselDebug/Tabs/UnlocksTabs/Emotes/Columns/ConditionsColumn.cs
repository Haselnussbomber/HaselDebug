using HaselCommon.Gui.ImGuiTable;
using static HaselDebug.Tabs.UnlocksTabs.Emotes.EmotesTable;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes.Columns;

// ["E8 ?? ?? ?? ?? 41 8B D6 48 8D 8E"] IsVisor = (emoteId - 60) <= 1
// ["E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 40 84 FF"] IsMimic = EmoteCategory == 3
// IsEmoteCategoryHidden = EmoteCategory == 4

[RegisterTransient]
public unsafe class ConditionsColumn : ColumnString<Emote>
{
    private readonly UldService _uldService;

    public ConditionsColumn(UldService uldService)
    {
        _uldService = uldService;

        SetFixedWidth(300);
    }

    public override string ToName(Emote row)
    {
        var conditions = GetEmoteConditions(row);
        return conditions.ToString();
    }

    public override void DrawColumn(Emote row)
    {
        var conditions = GetEmoteConditions(row);

        foreach (var condition in Enum.GetValues<EmoteCondition>())
        {
            if (condition is EmoteCondition.None)
                continue;

            var partId = condition switch
            {
                EmoteCondition.SittingOnGround => 0u,
                EmoteCondition.SittingInChair => 1u,
                EmoteCondition.Mounted => 2u,
                EmoteCondition.Fishing => 3u,
                EmoteCondition.Standing => 4u,
                EmoteCondition.Swimming => 5u,
                EmoteCondition.Diving => 6u,
                EmoteCondition.HoldingUmbrella => 7u,
                EmoteCondition.WearingFashionAccessory => 8u,
                EmoteCondition.HoldingTorch => 9u,
                _ => 0u,
            };

            _uldService.DrawPart("Emote", 15, partId, new DrawInfo(ImGui.GetTextLineHeightWithSpacing())
            {
                TintColor = conditions.HasFlag(condition) ? Color.White : Color.Text300
            });

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(condition.ToString());

            ImGui.SameLine(0, 0);
        }
    }
}
