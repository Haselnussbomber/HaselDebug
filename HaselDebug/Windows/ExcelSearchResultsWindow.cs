using HaselDebug.Tabs;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class ExcelSearchResultsWindow : SimpleWindow
{
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

    public override bool DrawConditions()
    {
        return true; // Always show window, even with 0 results
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

        foreach (var result in _results)
        {
            ImGui.TableNextRow();
            
            ImGui.TableNextColumn();
            ImGui.Text(result.SheetType);
            
            ImGui.TableNextColumn();
            if (ImGui.Selectable(result.SheetName, false, ImGuiSelectableFlags.SpanAllColumns))
            {
                _excelTab.ChangeSheetFromSearch(result.SheetName);
            }
            
            ImGui.TableNextColumn();
            ImGui.Text(result.RowId.ToString());
            
            ImGui.TableNextColumn();
            ImGui.Text(result.ColumnIndex.ToString());
            
            ImGui.TableNextColumn();
            ImGui.Text(result.ColumnName);
            
            ImGui.TableNextColumn();
            var displayValue = result.Value.Length > 100 ? result.Value[..100] + "..." : result.Value;
            ImGui.TextWrapped(displayValue);
        }
    }
}
