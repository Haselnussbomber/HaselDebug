using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Quests.Columns;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Quests;

[RegisterSingleton, AutoConstruct]
public partial class QuestsTable : Table<Quest>, IDisposable
{
    private readonly ExcelService _excelService;
    private readonly QuestIdColumn _questIdColumn;
    private readonly QuestStatusColumn _questStatusColumn;
    private readonly RepeatableColumn _repeatableColumn;
    private readonly CategoryColumn _categoryColumn;
    private readonly NameColumn _nameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Quest>.Create(),
            _questIdColumn,
            _questStatusColumn,
            _repeatableColumn,
            _categoryColumn,
            _nameColumn,
        ];

        Flags |= ImGuiTableFlags.Resizable; // TODO: no worky?
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Quest>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty).ToList();
    }
}
