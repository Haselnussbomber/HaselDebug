using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Sheets;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.OutfitsTableColumns;

public class RowIdColumn : ColumnNumber<CustomMirageStoreSetItem>
{
    public override void DrawColumn(CustomMirageStoreSetItem row)
    {
        //ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2f - ImGui.CalcTextSize(row.RowId.ToString()).X / 2f);
        ImGuiUtils.PushCursorY(ImGui.GetTextLineHeight() / 2f);
        ImGui.TextUnformatted(row.RowId.ToString());
    }

    public override int Compare(CustomMirageStoreSetItem a, CustomMirageStoreSetItem b)
    {
        return a.RowId.CompareTo(b.RowId);
    }
}
