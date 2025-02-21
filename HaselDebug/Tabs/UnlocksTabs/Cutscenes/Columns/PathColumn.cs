using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes.Columns;

[RegisterTransient]
public class PathColumn : ColumnString<CutsceneEntry>
{
    public PathColumn()
    {
        SetFixedWidth(315);
    }

    public override string ToName(CutsceneEntry entry)
        => entry.Row.Path.ExtractText();
}
