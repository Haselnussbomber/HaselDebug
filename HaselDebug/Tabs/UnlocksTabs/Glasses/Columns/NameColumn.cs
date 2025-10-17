using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;
using GlassesSheet = Lumina.Excel.Sheets.Glasses;

namespace HaselDebug.Tabs.UnlocksTabs.Glasses.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<GlassesSheet>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(GlassesSheet row)
        => _textService.GetGlassesName(row.RowId);

    public override unsafe void DrawColumn(GlassesSheet row)
    {
        _debugRenderer.DrawIcon((uint)row.Icon);

        var name = ToName(row);

        using (Color.Transparent.Push(ImGuiCol.HeaderActive))
        using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
            ImGui.Selectable(name);

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawTooltip(
                (uint)row.Icon,
                name,
                default,
                row.Description);
        }
    }
}
