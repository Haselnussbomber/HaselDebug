using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<HowTo>
{
    [AutoPostConstruct]
    private void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(HowTo row)
        => row.Name.ToString();

    public override unsafe void DrawColumn(HowTo row)
    {
        ImGui.Text(ToName(row));
    }
}
