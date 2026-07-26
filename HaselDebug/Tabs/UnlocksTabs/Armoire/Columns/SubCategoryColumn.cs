using HaselCommon.Gui.ImGuiTable;
using CabinetSheet = Lumina.Excel.Sheets.Cabinet;

namespace HaselDebug.Tabs.UnlocksTabs.Armoire.Columns;

[RegisterSingleton, AutoConstruct]
public partial class SubCategoryColumn : ColumnString<CabinetSheet>
{
    [AutoPostConstruct]
    public void Initialize()
    {
        SetStretchWidth(1);
        Flags |= ImGuiTableColumnFlags.DefaultSort;
    }

    public override string ToName(CabinetSheet row)
        => row.SubCategory.Value.Name.ToString();

    public override void DrawColumn(CabinetSheet row)
    {
        ImGui.Text(ToName(row));
    }
}
