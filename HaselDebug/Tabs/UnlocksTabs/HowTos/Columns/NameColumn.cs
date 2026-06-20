using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<HowTo>
{
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    private void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(HowTo row)
        => row.Name.ToString();

    public override unsafe void DrawColumn(HowTo row)
    {
        var isUnlocked = UIState.Instance()->IsHowToUnlocked(row.RowId);

        if (ImGui.Selectable(ToName(row), false, isUnlocked ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled))
        {
            AgentHowTo.Instance()->OpenHowTo(row.RowId);
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (isUnlocked)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            _unlocksTabUtils.DrawHowToTooltip(row);
        }
    }
}
