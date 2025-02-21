using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes.Columns;

[RegisterTransient]
public class WorkIndexColumn : ColumnNumber<CutsceneEntry>
{
    public WorkIndexColumn()
    {
        SetFixedWidth(60);
    }

    public override string ToName(CutsceneEntry entry)
        => entry.WorkIndexRow.WorkIndex.ToString();

    public override int ToValue(CutsceneEntry entry)
        => entry.WorkIndexRow.WorkIndex;
}
