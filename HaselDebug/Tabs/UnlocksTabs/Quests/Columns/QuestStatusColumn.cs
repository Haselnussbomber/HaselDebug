using System.Linq;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Quests.Columns;

[RegisterTransient]
public class QuestStatusColumn : ColumnFlags<QuestStatus, Quest>
{
    private QuestStatus _filterValue;
    public override QuestStatus FilterValue => _filterValue;

    public QuestStatusColumn()
    {
        SetFixedWidth(75);
        AllFlags = Enum.GetValues<QuestStatus>().Aggregate((a, b) => a | b);
        _filterValue = AllFlags;
    }

    public virtual unsafe QuestStatus ToStatus(Quest row)
    {
        if (QuestManager.Instance()->IsQuestAccepted(row.RowId))
            return QuestStatus.Accepted;

        if (QuestManager.IsQuestComplete(row.RowId))
            return QuestStatus.Complete;

        return QuestStatus.Incomplete;
    }

    public override bool ShouldShow(Quest row)
    {
        var value = ToStatus(row);
        return (FilterValue.HasFlag(QuestStatus.Accepted) && value == QuestStatus.Accepted) ||
               (FilterValue.HasFlag(QuestStatus.Complete) && value == QuestStatus.Complete) ||
               (FilterValue.HasFlag(QuestStatus.Incomplete) && value == QuestStatus.Incomplete);
    }

    public override unsafe void DrawColumn(Quest row)
    {
        var value = ToStatus(row);
        using (ImRaii.PushColor(ImGuiCol.Text, (value == QuestStatus.Complete ? Color.Green : value == QuestStatus.Accepted ? Color.Yellow : Color.Red).ToUInt()))
            ImGui.TextUnformatted(Enum.GetName(value));
    }

    public override unsafe int Compare(Quest a, Quest b)
        => ToStatus(a).CompareTo(ToStatus(b));

    public override void SetValue(QuestStatus value, bool enable)
    {
        if (enable)
            _filterValue |= value;
        else
            _filterValue &= ~value;
    }
}
