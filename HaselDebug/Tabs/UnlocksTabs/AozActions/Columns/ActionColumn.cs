using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions.Columns;

[RegisterTransient, AutoConstruct]
public partial class ActionColumn : ColumnString<AozEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly ISeStringEvaluator _seStringEvaluator;

    public override string ToName(AozEntry entry)
        => entry.Action.Name.ToString();

    public override unsafe void DrawColumn(AozEntry entry)
    {
        ImGui.BeginGroup();
        _debugRenderer.DrawIcon(entry.AozActionTransient.Icon, noTooltip: true);
        ImGuiUtils.DrawCopyableText(entry.Action.Name.ToString(), new() { NoTooltip = true });
        ImGui.EndGroup();

        if (ImGui.IsItemHovered())
        {
            using var rssb = new RentedSeStringBuilder();
            var sb = rssb.Builder;
            sb.Append(entry.AozActionTransient.Description);
            sb.AppendNewLine();
            sb.Append(entry.AozActionTransient.Stats);
            _unlocksTabUtils.DrawTooltip(
                entry.AozActionTransient.Icon,
                entry.Action.Name,
                _seStringEvaluator.EvaluateFromAddon(12262, [(uint)entry.AozActionTransient.Number]),
                sb.ToReadOnlySeString());
        }
    }
}
