using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls.Columns;

[RegisterTransient, AutoConstruct]
public partial class CategoryColumn : ColumnString<OrchestrionRollEntry>
{
    private readonly DebugRenderer _debugRenderer;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(265);
        LabelKey = "CategoryColumn.Label";
    }

    public override string ToName(OrchestrionRollEntry entry)
        => entry.UIParamRow.OrchestrionCategory.Value.Name.ExtractText().StripSoftHyphen();

    public override void DrawColumn(OrchestrionRollEntry entry)
    {
        _debugRenderer.DrawIcon(entry.UIParamRow.OrchestrionCategory.Value.Icon);
        ImGui.TextUnformatted(ToName(entry));
    }

    public override int Compare(OrchestrionRollEntry a, OrchestrionRollEntry b)
    {
        var result = base.Compare(a, b);
        if (result == 0)
            result = a.UIParamRow.Order.CompareTo(b.UIParamRow.Order);
        return result;
    }
}
