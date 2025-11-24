namespace HaselDebug.Tabs.Excel;

[AutoConstruct]
public partial class ExcelV2SheetWrapper<T> : IExcelV2SheetWrapper where T : struct
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Excel2Tab _excelTab;

    private ExcelTable<T> _table;

    public List<ExcelV2SheetColumn<T>> Columns { get; set; } = [];
    public string SheetName { get; } = typeof(T).Name;
    public ClientLanguage Language => _excelTab.SelectedLanguage;

    [AutoPostConstruct]
    private void Initialize()
    {
        _table = ActivatorUtilities.CreateInstance<ExcelTable<T>>(_serviceProvider, _excelTab);
        ReloadSheet();
    }

    public void ReloadSheet()
    {
        _table.RowsLoaded = false;
    }

    public void Draw()
    {
        ImGui.Text(SheetName);
        ImGui.SameLine();

        var count = (_table.FilteredRows ?? _table.Rows).Count;
        ImGui.Text($"{count} row{(count != 1 ? "s" : "")}");
        ImGui.Text($"IsSubrowType: {_table.IsSubrowType}");

        ImGui.SameLine();
        ShowColumnSelector();

        using (ImRaii.PushId(SheetName))
            _table.Draw();
    }

    private void ShowColumnSelector()
    {
        if (ImGui.Button($"{_table.Columns.Count} of {_table.AvailableColumns.Count} columns shown"))
        {
            ImGui.OpenPopup("ColumnsPopup");
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _table.Columns.RemoveAll(col => col.Label is not ("RowId" or "SubrowId"));
        }

        using var contextMenu = ImRaii.Popup("ColumnsPopup");
        if (!contextMenu) return;

        var canAdd = _table.Columns.Count < Excel2Tab.MaxColumns;

        for (var i = 0; i < _table.AvailableColumns.Count; i++)
        {
            var column = _table.AvailableColumns[i];
            var selected = _table.Columns.Any(c => c.Label == column.Label);

            using var disabled = column.Label is "RowId" or "SubrowId" ? ImRaii.Disabled() : null;

            if (ImGui.MenuItem($"[{column.ColumnTypeName}] {column.Label}##{column.Label}Select", string.Empty, selected, selected || canAdd))
            {
                if (!selected)
                {
                    _table.Columns.Add(column);
                }
                else
                {
                    _table.Columns.RemoveAll(col => col.Label == column.Label);
                }
            }
        }
    }
}
