using HaselDebug.Tabs.Excel;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class ExcelSearchResultsWindow : SimpleWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WindowManager _windowManager;
    private readonly Excel2Tab _excelTab;
    private readonly string _searchTerm;
    private readonly List<GlobalSearchResult> _results;

    [AutoPostConstruct]
    private void Initialize(string windowName)
    {
        WindowName = windowName;
    }

    public override void OnOpen()
    {
        base.OnOpen();

        Size = new Vector2(1000, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Appearing;

        Flags |= ImGuiWindowFlags.NoSavedSettings;

        RespectCloseHotkey = true;
        DisableWindowSounds = true;
    }

    public override void Draw()
    {
        if (_results.Count == 0)
        {
            ImGui.TextWrapped($"No results found for \"{_searchTerm}\"");
            return;
        }

        using var table = ImRaii.Table("SearchResultsTable", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.Sortable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Sheet", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("Row ID", ImGuiTableColumnFlags.WidthFixed, 75);
        ImGui.TableSetupColumn("Column Index", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Column Name", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, result) in _results.Index())
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(result.SheetType);

            ImGui.TableNextColumn();
            if (ImGui.Selectable($"{result.SheetName}##SheetName{index}", false))
            {
                _excelTab.ChangeSheet(result.SheetName);
            }

            ImGui.TableNextColumn();
            if (result.IsSubrowSheet)
            {
                ImGui.Text($"{result.RowId}");
            }
            else if (ImGui.Selectable($"{result.RowId}##RowId{index}", false))
            {
                OpenSheet(result.SheetName, (uint)float.Parse(result.RowId));
            }

            ImGui.TableNextColumn();
            ImGui.Text(result.ColumnIndex.ToString());

            ImGui.TableNextColumn();
            ImGui.Text(result.ColumnName);

            ImGui.TableNextColumn();
            var displayValue = result.Value.Length > 100 ? result.Value[..100] + "..." : result.Value;
            ImGui.TextWrapped(displayValue);
        }
    }

    private void OpenSheet(string sheetName, uint rowId)
    {
        if (!_excelTab.TryGetSheetType(sheetName, out var sheetType))
            return;

        var title = $"{sheetName}#{rowId} ({_excelTab.SelectedLanguage})";
        _windowManager.CreateOrOpen(title, () => ActivatorUtilities.CreateInstance<ExcelRowTab>(_serviceProvider, sheetType, rowId, _excelTab.SelectedLanguage, title));
    }
}
