using System.Globalization;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Data.Structs.Excel;

namespace HaselDebug.Tabs.Excel;

[AutoConstruct]
public partial class RawSheetWrapper : IExcelV2SheetWrapper
{
    private readonly Excel2Tab _excelTab;
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;

    private List<RawRow> _rows = [];
    private List<RawRow> _filteredRows = [];
    private IReadOnlyList<ExcelColumnDefinition> _columns = [];

    [AutoConstructIgnore]
    public string SheetName { get; private set; } = string.Empty;

    public ClientLanguage Language => _excelTab.SelectedLanguage;

    [AutoPostConstruct]
    private void Initialize(string sheetName)
    {
        SheetName = sheetName;
    }

    public void ReloadSheet()
    {
        _rows.Clear();
        _filteredRows.Clear();
        _columns = [];

        // Fetch the raw sheet data for the selected language
        var sheet = _excelService.GetSheet<RawRow>(SheetName, Language);
        if (sheet == null)
            return;

        _rows = [.. sheet];
        _filteredRows = _rows;
        _columns = _rows.ElementAt(0).Columns;
    }

    public void Draw()
    {
        // Only fetch sheet data when first drawn (not when ChangeSheet is called)
        if (_rows.Count == 0)
            ReloadSheet();

        // Header (SheetName, Type, RowCount, ColumnCount)
        ImGui.Text(SheetName);
        ImGui.SameLine();
        using (Color.Grey.Push(ImGuiCol.Text))
            ImGui.Text(" (Raw Sheet)");
        ImGui.SameLine();
        ImGui.Text($"{_filteredRows.Count} row{(_filteredRows.Count != 1 ? "s" : "")}");
        ImGui.SameLine();
        ImGui.Text($"{_columns.Count} column{(_columns.Count != 1 ? "s" : "")}");

        var visibleColumnCount = Math.Min(_columns.Count, 100);

        // Create table with +1 column for RowId, plus all data columns
        using var table = ImRaii.Table("RawSheetTable", visibleColumnCount + 1,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.ScrollX | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings,
            new Vector2(-1));
        if (!table) return;

        // Set up columns
        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 75);
        ImGui.TableSetupScrollFreeze(1, 1);

        // Create a column for each detected data column
        for (var i = 0; i < visibleColumnCount; i++)
        {
            ImGui.TableSetupColumn($"Column{i}", ImGuiTableColumnFlags.WidthFixed, 100);
        }
        ImGui.TableHeadersRow();

        // Draw rows
        foreach (var row in _filteredRows)
        {
            ImGui.TableNextRow();

            // RowId column
            ImGui.TableNextColumn();
            ImGui.Text(row.RowId.ToString());

            // Data columns
            for (var i = 0; i < visibleColumnCount; i++)
            {
                ImGui.TableNextColumn();
                var column = _columns[i];

                if (column.Type == ExcelColumnDataType.String)
                {
                    var stringValue = row.ReadStringColumn(i);
                    if (!stringValue.IsEmpty)
                    {
                        _debugRenderer.DrawSeString(stringValue.AsSpan(), new NodeOptions()
                        {
                            RenderSeString = false,
                            Title = $"{SheetName}#{row.RowId} Column{i}",
                            Language = Language,
                        });
                    }
                }
                else if (column.Type == ExcelColumnDataType.Bool)
                {
                    ImGui.Text(row.ReadBoolColumn(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.Int8)
                {
                    ImGui.Text(row.ReadInt8Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.Int16)
                {
                    ImGui.Text(row.ReadInt16Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.Int32)
                {
                    ImGui.Text(row.ReadInt32Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.Int64)
                {
                    ImGui.Text(row.ReadInt64Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.UInt8)
                {
                    ImGui.Text(row.ReadUInt8Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.UInt16)
                {
                    ImGui.Text(row.ReadUInt16Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.UInt32)
                {
                    ImGui.Text(row.ReadUInt32Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.UInt64)
                {
                    ImGui.Text(row.ReadUInt64Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.Float32)
                {
                    ImGui.Text(row.ReadFloat32Column(i).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool0)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 0).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool1)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 1).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool2)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 2).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool3)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 3).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool4)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 4).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool5)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 5).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool6)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 6).ToString(CultureInfo.InvariantCulture));
                }
                else if (column.Type == ExcelColumnDataType.PackedBool7)
                {
                    ImGui.Text(row.ReadPackedBoolColumn(i, 7).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    ImGui.Text($"Unknown Type {column.Type}");
                }
            }
        }
    }
}
