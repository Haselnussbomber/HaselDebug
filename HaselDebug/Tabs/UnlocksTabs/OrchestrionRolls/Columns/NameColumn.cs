using Dalamud.Utility;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<OrchestrionRollEntry>
{
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(OrchestrionRollEntry entry)
        => entry.Row.Name.ExtractText().StripSoftHyphen();

    public override unsafe void DrawColumn(OrchestrionRollEntry entry)
    {
        var name = ToName(entry);
        using (Color.Transparent.Push(ImGuiCol.HeaderActive))
        using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
            ImGui.Selectable(name);

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawTooltip(
                entry.UIParamRow.OrchestrionCategory.Value.Icon,
                name,
                entry.UIParamRow.OrchestrionCategory.Value.Name,
                !entry.Row.Description.IsEmpty
                    ? entry.Row.Description
                    : default);
        }
    }
}
