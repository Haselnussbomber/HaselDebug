using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Bardings.Columns;

[RegisterTransient, AutoConstruct]
public partial class ItemColumn : ColumnString<BuddyEquip>
{
    private readonly DebugRenderer _debugRenderer;

    public override string ToName(BuddyEquip row)
        => row.Name.ToString();

    public override unsafe void DrawColumn(BuddyEquip row)
    {
        _debugRenderer.DrawIcon(row.IconBody != 0
            ? row.IconBody
            : row.IconHead != 0
                ? row.IconHead
                : row.IconLegs);
        ImGui.Text(row.Name.ToString());
    }
}
