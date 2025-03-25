using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Quests.Columns;

[RegisterTransient, AutoConstruct]
public partial class CategoryColumn : ColumnString<Quest>
{
    private readonly DebugRenderer _debugRenderer;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(250);
        LabelKey = "CategoryColumn.Label";
    }

    public override string ToName(Quest row)
    {
        if (row.JournalGenre.RowId == 0 || !row.JournalGenre.IsValid)
            return string.Empty;

        return row.JournalGenre.Value.Name.ExtractText().StripSoftHyphen();
    }

    public override void DrawColumn(Quest row)
    {
        if (row.JournalGenre.RowId != 0 && row.JournalGenre.IsValid)
        {
            _debugRenderer.DrawIcon((uint)row.JournalGenre.Value.Icon);
            ImGui.TextUnformatted(ToName(row));
        }
    }
}
