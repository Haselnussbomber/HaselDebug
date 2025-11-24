using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Quests.Columns;

[RegisterTransient]
public class QuestIdColumn : ColumnNumber<Quest>
{
    public QuestIdColumn()
    {
        SetFixedWidth(60);
    }

    public override int ToValue(Quest row)
        => (int)(row.RowId - 0x10000);

    public override void DrawColumn(Quest row)
        => ImGuiUtils.DrawCopyableText(ToName(row));
}
