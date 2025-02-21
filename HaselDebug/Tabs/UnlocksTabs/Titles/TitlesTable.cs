using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Titles.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Titles;

[RegisterSingleton, AutoConstruct]
public partial class TitlesTable : Table<Title>
{
    private readonly ExcelService _excelService;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly PrefixColumn _prefixColumn;
    private readonly TitleColumn _masculineTitleColumn;
    private readonly TitleColumn _feminineTitleColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            RowIdColumn<Title>.Create(),
            _unlockedColumn,
            _prefixColumn,
            _masculineTitleColumn,
            _feminineTitleColumn,
        ];

        _masculineTitleColumn.SetSex(false);
        _feminineTitleColumn.SetSex(true);
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Title>()
            .Where(row => row.RowId != 0 && !row.Feminine.IsEmpty && !row.Masculine.IsEmpty)
            .ToList();
    }
}
