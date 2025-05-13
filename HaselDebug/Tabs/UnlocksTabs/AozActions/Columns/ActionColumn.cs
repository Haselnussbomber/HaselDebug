using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions.Columns;

[RegisterTransient, AutoConstruct]
public partial class ActionColumn : ColumnString<AozEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly SeStringEvaluator _seStringEvaluator;

    public override string ToName(AozEntry entry)
        => entry.Action.Name.ExtractText();

    public override unsafe void DrawColumn(AozEntry entry)
    {
        ImGui.BeginGroup();
        _debugRenderer.DrawIcon(entry.AozActionTransient.Icon, noTooltip: true);
        _debugRenderer.DrawCopyableText(entry.Action.Name.ExtractText(), noTooltip: true);
        ImGui.EndGroup();

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawTooltip(
                entry.AozActionTransient.Icon,
                entry.Action.Name,
                _seStringEvaluator.EvaluateFromAddon(12262, [(uint)entry.AozActionTransient.Number]),
                entry.AozActionTransient.Description);
        }
    }
}
