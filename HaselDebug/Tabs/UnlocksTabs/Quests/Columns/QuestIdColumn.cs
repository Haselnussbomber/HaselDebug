using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Quests.Columns;

[RegisterTransient, AutoConstruct]
public partial class QuestIdColumn : ColumnNumber<Quest>
{
    private readonly DebugRenderer _debugRenderer;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(60);
    }

    public override int ToValue(Quest row)
        => (int)(row.RowId - 0x10000);

    public override void DrawColumn(Quest row)
        => _debugRenderer.DrawCopyableText(ToName(row));
}
