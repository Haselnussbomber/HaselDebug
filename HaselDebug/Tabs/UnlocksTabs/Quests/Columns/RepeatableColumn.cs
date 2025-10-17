using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Quests.Columns;

[RegisterTransient]
public class RepeatableColumn : ColumnBool<Quest>
{
    public RepeatableColumn()
    {
        SetFixedWidth(75);
    }

    public override unsafe bool ToBool(Quest row)
        => row.IsRepeatable;

    public override unsafe void DrawColumn(Quest row)
        => ImGui.Text(Names[ToBool(row) ? 1 : 0]);
}
