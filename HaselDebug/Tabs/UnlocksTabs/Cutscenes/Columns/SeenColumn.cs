using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes.Columns;

[RegisterTransient]
public class SeenColumn : ColumnBool<CutsceneEntry>
{
    public SeenColumn()
    {
        SetFixedWidth(75);
    }

    public override unsafe bool ToBool(CutsceneEntry entry)
        => UIState.Instance()->IsCutsceneSeen((uint)entry.Index);
}
