using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.TitlesTableColumns;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs;

[RegisterSingleton]
public class TitlesTable : Table<Title>
{
    public TitlesTable(ExcelService excelService, LanguageProvider languageProvider) : base("TitlesTable", languageProvider)
    {
        Rows = excelService.GetSheet<Title>()
            .Where(row => row.RowId != 0 && !row.Feminine.IsEmpty && !row.Masculine.IsEmpty)
            .ToList();

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new PrefixColumn() {
                Label = "IsPrefix",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new TitleColumn(true),
            new TitleColumn(false),
        ];
    }
}
