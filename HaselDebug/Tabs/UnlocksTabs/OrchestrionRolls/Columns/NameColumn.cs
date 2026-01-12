using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<OrchestrionRollEntry>
{
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly TextService _textService;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(OrchestrionRollEntry entry)
        => entry.Row.Name.ToString();

    public override unsafe void DrawColumn(OrchestrionRollEntry entry)
    {
        var name = ToName(entry);
        var selected = OrchestrionManager.Instance()->Mode == OrchestrionMode.Sampling && OrchestrionSampleState.Instance()->TrackId == entry.RowId;
        var isUnlocked = PlayerState.Instance()->IsOrchestrionRollUnlocked(entry.RowId);
        var flags = isUnlocked
            ? ImGuiSelectableFlags.None
            : ImGuiSelectableFlags.Disabled;

        if (ImGui.Selectable(name, selected, flags))
        {
            if (selected)
            {
                OrchestrionManager.StopSample();
            }
            else
            {
                OrchestrionManager.PlaySample((ushort)entry.RowId);
            }
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using var rssb = new RentedSeStringBuilder();

            if (!entry.Row.Description.IsEmpty)
            {
                rssb.Builder.AppendLine(entry.Row.Description);
                rssb.Builder.AppendNewLine();
            }

            if (isUnlocked)
                rssb.Builder.Append("Click to preview track");
            else
                rssb.Builder.Append(_textService.GetLogMessage(3437));

            _unlocksTabUtils.DrawTooltip(
                entry.UIParamRow.OrchestrionCategory.Value.Icon,
                name,
                entry.UIParamRow.OrchestrionCategory.Value.Name,
                rssb.Builder.ToReadOnlySeString());
        }
    }
}
