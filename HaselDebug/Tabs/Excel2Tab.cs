using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Tabs;

#pragma warning disable PendingExcelSchema

public record GlobalSearchResult(string SheetType, string SheetName, uint RowId, int ColumnIndex, string ColumnName, string Value);

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class Excel2Tab : DebugTab
{
    public const int MaxColumns = 60;
    private const int LanguageSelectorWidth = 90;

    private readonly IServiceProvider _serviceProvider;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly IDataManager _dataManager;
    private readonly ExcelService _excelService;
    private readonly WindowManager _windowManager;
    private readonly ILogger<Excel2Tab> _logger;

    private Dictionary<string, Type> _sheetTypes;
    private HashSet<string> _allSheetNames;
    private IExcelV2SheetWrapper? _sheetWrapper;
    private IExcelV2SheetWrapper? _nextSheetWrapper;
    private string _sheetNameSearchTerm = string.Empty;
    private bool _useExperimentalSheets = true;
    private bool _showUntypedSheets = false;
    private bool _isInitialized;
    
    private string _globalSearchTerm = string.Empty;
    private List<GlobalSearchResult> _globalSearchResults = new();
    private bool _isSearching = false;
    private bool _openResultsWindowOnNextFrame = false;
    private CancellationTokenSource? _searchCts;

    public override string Title => "Excel (v2)";

    public string SearchTerm { get; private set; } = string.Empty;
    public ClientLanguage SelectedLanguage { get; private set; }

    private void Initialize()
    {
        SelectedLanguage = _languageProvider.ClientLanguage;
        LoadSheetTypes();
    }

    private void LoadSheetTypes()
    {
        var sheetsType = _useExperimentalSheets
            ? typeof(Lumina.Excel.Sheets.Experimental.Achievement)
            : typeof(Lumina.Excel.Sheets.Achievement);

        _sheetTypes = sheetsType.Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == sheetsType.Namespace && !string.IsNullOrEmpty(type.GetCustomAttribute<SheetAttribute>()?.Name))
            .ToDictionary(type => type.GetCustomAttribute<SheetAttribute>()!.Name!);

        // Load all sheet names from ExcelListFile
        try
        {
            var excelListFile = _dataManager.GameData.GetFile<Lumina.Data.Files.Excel.ExcelListFile>("exd/root.exl");
            if (excelListFile != null)
            {
                _allSheetNames = excelListFile.ExdMap.Keys.ToHashSet();
            }
            else
            {
                _allSheetNames = _sheetTypes.Keys.ToHashSet();
            }
        }
        catch
        {
            _allSheetNames = _sheetTypes.Keys.ToHashSet();
        }

        ChangeSheet(_nextSheetWrapper?.SheetName ?? _sheetWrapper?.SheetName ?? "Achievement");
    }

    public override bool DrawInChild => false;
    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        using var hostChild = ImRaii.Child("Host", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);
        if (!hostChild) return;

        ImGui.Text("Work in progress!"u8);

        // Open results window on main thread if flagged
        if (_openResultsWindowOnNextFrame)
        {
            _openResultsWindowOnNextFrame = false;
            try
            {
                _logger.LogInformation("[Excel2Tab] Opening search results window from main thread");
                OpenSearchResultsWindow();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Excel2Tab] Failed to open search results window from main thread");
            }
        }

        if (_nextSheetWrapper != null)
        {
            _sheetWrapper = _nextSheetWrapper;
            _nextSheetWrapper = null;
        }

        /*
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - LanguageSelectorWidth * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X);
        var searchTerm = SearchTerm;
        var listDirty = false;
        if (ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll))
        {
            SearchTerm = searchTerm;
            listDirty |= true;
        }
        */

        ImGui.SameLine();

        ImGui.SetNextItemWidth(LanguageSelectorWidth * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language", SelectedLanguage.ToString()))
        {
            if (dropdown)
            {
                var values = Enum.GetValues<ClientLanguage>().OrderBy((ClientLanguage lang) => lang.ToString());
                foreach (var value in values)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == SelectedLanguage))
                    {
                        SelectedLanguage = value;
                        _sheetWrapper?.ReloadSheet();
                    }
                }
            }
        }

        ImGui.SameLine();

        if (ImGui.Checkbox("Use Experimental Sheets", ref _useExperimentalSheets))
        {
            LoadSheetTypes();
        }

        ImGui.SameLine();
        ImGui.Checkbox("Show Untyped Sheets", ref _showUntypedSheets);

        DrawGlobalSearch();

        DrawSheetList();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var innerChild = ImRaii.Child("InnerHost", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);
        if (!innerChild) return;

        if (_sheetWrapper == null)
        {
            ImGui.Text("No sheet selected."u8);
        }
        else
        {
            _sheetWrapper.Draw();
        }
    }

    private void DrawSheetList()
    {
        using var sidebarchild = ImRaii.Child("SheetListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(-1);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##NameTextSearch", _textService.Translate("SearchBar.Hint"), ref _sheetNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_sheetNameSearchTerm);

        // TODO: checkbox Search Rows

        using var table = ImRaii.Table("SheetTable"u8, 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(1, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        foreach (var sheetName in _allSheetNames.OrderBy(sheetName => sheetName))
        {
            if (hasSearchTerm && !sheetName.Contains(_sheetNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            var hasType = _sheetTypes.ContainsKey(sheetName);
            
            // Skip untyped sheets if the checkbox is not enabled
            if (!hasType && !_showUntypedSheets)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Name
            
            if (!hasType)
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, 0xFF666666);
            }
            
            if (ImGui.Selectable(sheetName + $"###SheetSelectable{i++}", sheetName == _sheetWrapper?.SheetName, ImGuiSelectableFlags.SpanAllColumns))
            {
                ChangeSheet(sheetName);
            }
        }
    }

    private void ChangeSheet(string sheetName)
    {
        if (!TryGetSheetType(sheetName, out var sheetType))
            return;

        _nextSheetWrapper = (IExcelV2SheetWrapper)ActivatorUtilities.CreateInstance(
            _serviceProvider,
            typeof(ExcelV2SheetWrapper<>).MakeGenericType(sheetType),
            this);
    }

    public bool TryGetSheetType(string sheetName, [NotNullWhen(returnValue: true)] out Type? sheetType)
        => _sheetTypes.TryGetValue(sheetName, out sheetType);

    public void ChangeSheetFromSearch(string sheetName)
    {
        ChangeSheet(sheetName);
    }

    private void DrawGlobalSearch()
    {
        ImGui.Separator();
        ImGui.Text("Search All Sheets:");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(300);
        ImGui.InputTextWithHint("##GlobalSearch", "Enter search term...", ref _globalSearchTerm, 256);
        
        ImGui.SameLine();
        using (ImRaii.Disabled(_isSearching || string.IsNullOrWhiteSpace(_globalSearchTerm)))
        {
            if (ImGui.Button("Search"))
            {
                StartGlobalSearch();
            }
        }
        
        if (_isSearching)
        {
            ImGui.SameLine();
            ImGui.Text("Searching...");
        }
        
        if (_globalSearchResults.Count > 0)
        {
            ImGui.SameLine();
            ImGui.Text($"{_globalSearchResults.Count} result(s) found");
            
            ImGui.SameLine();
            if (ImGui.Button("View Results"))
            {
                OpenSearchResultsWindow();
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Clear Results"))
            {
                _globalSearchResults.Clear();
            }
        }
        
        ImGui.Separator();
    }

    private void OpenSearchResultsWindow()
    {
        _logger.LogInformation("[Excel2Tab] OpenSearchResultsWindow called with {ResultCount} results", _globalSearchResults.Count);
        var windowTitle = $"Excel Search Results - \"{_globalSearchTerm}\" ({_globalSearchResults.Count} results)";
        _logger.LogInformation("[Excel2Tab] Window title: {WindowTitle}", windowTitle);
        
        try
        {
            _windowManager.CreateOrOpen(windowTitle, () => 
            {
                _logger.LogInformation("[Excel2Tab] Creating ExcelSearchResultsWindow instance");
                return ActivatorUtilities.CreateInstance<ExcelSearchResultsWindow>(
                    _serviceProvider,
                    this,
                    _globalSearchTerm,
                    new List<GlobalSearchResult>(_globalSearchResults),
                    windowTitle
                );
            });
            _logger.LogInformation("[Excel2Tab] CreateOrOpen completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Excel2Tab] Failed in CreateOrOpen");
            throw;
        }
    }

    private void StartGlobalSearch()
    {
        if (string.IsNullOrWhiteSpace(_globalSearchTerm))
        {
            _logger.LogWarning("[Excel2Tab] Search term is empty");
            return;
        }

        _logger.LogInformation("[Excel2Tab] Starting global search for: {SearchTerm}", _globalSearchTerm);

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        var searchTerm = _globalSearchTerm;

        _isSearching = true;
        _globalSearchResults.Clear();

        _logger.LogInformation("[Excel2Tab] Searching {SheetCount} sheets (all sheets including untyped)", _allSheetNames.Count);

        Task.Run(() =>
        {
            try
            {
                var results = new List<GlobalSearchResult>();
                var sheetsSearched = 0;

                foreach (var sheetName in _allSheetNames.OrderBy(s => s))
                {
                    if (token.IsCancellationRequested)
                    {
                        _logger.LogInformation("[Excel2Tab] Search cancelled");
                        break;
                    }

                    try
                    {
                        // Try to search with type definition first, fall back to RawRow
                        if (TryGetSheetType(sheetName, out var sheetType))
                        {
                            SearchSheet(sheetType, sheetName, searchTerm, results, token);
                        }
                        else
                        {
                            SearchSheetRaw(sheetName, searchTerm, results, token);
                        }
                        sheetsSearched++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Excel2Tab] Failed to search sheet: {SheetName}", sheetName);
                    }
                }

                _logger.LogInformation("[Excel2Tab] Searched {SheetsSearched} sheets, found {ResultCount} results", sheetsSearched, results.Count);

                if (!token.IsCancellationRequested)
                {
                    _globalSearchResults = results;
                    
                    _logger.LogInformation("[Excel2Tab] Results stored, setting flag to open window on next frame");
                    
                    // Set flag to open window on next frame (main thread)
                    _openResultsWindowOnNextFrame = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Excel2Tab] Search failed with exception");
            }
            finally
            {
                _isSearching = false;
                _logger.LogInformation("[Excel2Tab] Search completed, _isSearching set to false");
            }
        }, token);
    }

    private void SearchSheet(Type sheetType, string sheetName, string searchTerm, List<GlobalSearchResult> results, CancellationToken token)
    {
        var getSheetMethodInfo = typeof(ExcelService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(mi => mi.Name == "GetSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1);
        
        var getSheetTyped = getSheetMethodInfo.MakeGenericMethod(sheetType);
        var sheet = getSheetTyped.Invoke(_excelService, [SelectedLanguage]);
        
        if (sheet == null)
            return;

        var rows = (System.Collections.IEnumerable)sheet;
        
        var properties = sheetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray();

        foreach (var row in rows)
        {
            if (token.IsCancellationRequested)
                break;

            var rowIdProp = properties.FirstOrDefault(p => p.Name == "RowId");
            var rowId = rowIdProp != null ? (uint)rowIdProp.GetValue(row)! : 0u;

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var value = prop.GetValue(row);
                
                if (value == null)
                    continue;

                string stringValue;
                if (prop.PropertyType == typeof(ReadOnlySeString))
                {
                    stringValue = ((ReadOnlySeString)value).ToString();
                }
                else
                {
                    stringValue = value.ToString() ?? string.Empty;
                }

                if (stringValue.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                {
                    results.Add(new GlobalSearchResult(
                        "Typed",
                        sheetName,
                        rowId,
                        i,
                        prop.Name,
                        stringValue
                    ));
                }
            }
        }
    }

    private void SearchSheetRaw(string sheetName, string searchTerm, List<GlobalSearchResult> results, CancellationToken token)
    {
        var sheet = _excelService.GetSheet<RawRow>(sheetName, SelectedLanguage);
        
        if (sheet == null)
            return;

        foreach (var row in sheet)
        {
            if (token.IsCancellationRequested)
                break;

            var rowId = row.RowId;
            
            // Try to determine column count by reading until we hit an exception
            // RawRow doesn't expose column count directly
            for (int i = 0; i < 100; i++) // reasonable max column limit
            {
                try
                {
                    // Try reading as string first
                    var stringValue = row.ReadStringColumn(i);
                    var stringText = stringValue.ToString();
                    if (!string.IsNullOrEmpty(stringText) && stringText.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                    {
                        results.Add(new GlobalSearchResult(
                            "Raw",
                            sheetName,
                            rowId,
                            i,
                            $"Column{i}",
                            stringText
                        ));
                        continue;
                    }

                    // Try reading as numeric types
                    try
                    {
                        var numValue = row.ReadUInt32Column(i);
                        var numString = numValue.ToString();
                        if (numString.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                        {
                            results.Add(new GlobalSearchResult(
                                "Raw",
                                sheetName,
                                rowId,
                                i,
                                $"Column{i}",
                                numString
                            ));
                        }
                    }
                    catch
                    {
                        // Not a numeric column, skip
                    }
                }
                catch
                {
                    // Column doesn't exist, we've reached the end
                    break;
                }
            }
        }
    }
}

public interface IExcelV2SheetWrapper
{
    string SheetName { get; }
    ClientLanguage Language { get; }
    void Draw();
    void ReloadSheet();
}

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

[AutoConstruct]
public partial class ExcelTable<T> : Table<T> where T : struct
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Excel2Tab _excelTab;
    private readonly ExcelService _excelService;

    public List<ExcelV2SheetColumn<T>> AvailableColumns { get; private set; } = [];
    public bool IsSubrowType { get; private set; }

    [AutoPostConstruct]
    public void Initialize()
    {
        Flags |= ImGuiTableFlags.Borders;
        Flags |= ImGuiTableFlags.ScrollX;
        Flags |= ImGuiTableFlags.Resizable;
        Flags |= ImGuiTableFlags.Reorderable;
        Flags |= ImGuiTableFlags.NoSavedSettings;
        Flags &= ~ImGuiTableFlags.NoBordersInBodyUntilResize;

        IsSubrowType = typeof(T).IsAssignableTo(typeof(IExcelSubrow<T>));

        if (IsSubrowType) ScrollFreezeCols = 2;

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var column = ActivatorUtilities.CreateInstance<ExcelV2SheetColumn<T>>(_serviceProvider, _excelTab, this, property);

            AvailableColumns.Add(column);

            if (Columns.Count < Excel2Tab.MaxColumns)
            {
                Columns.Add(column);
            }
        }
    }

    public override void LoadRows()
    {
        if (IsSubrowType)
        {
            // Rows = [.. _excelService.GetSubrowSheet<T>(_excelTab.SelectedLanguage).SelectMany(row => row)];

            var getSheetMethodInfo = typeof(ExcelService)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name == "GetSubrowSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1)
                .First();
            var getSheetTyped = getSheetMethodInfo.MakeGenericMethod(typeof(T));
            var collection = (System.Collections.IEnumerable)getSheetTyped.Invoke(_excelService, [_excelTab.SelectedLanguage])!;
            Rows = [.. collection.Cast<IReadOnlyList<T>>().SelectMany(row => row)];
        }
        else
        {
            // Rows = [.. _excelService.GetSheet<T>(_excelTab.SelectedLanguage)];

            var getSheetMethodInfo = typeof(ExcelService)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name == "GetSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1)
                .First();
            var getSheetTyped = getSheetMethodInfo.MakeGenericMethod(typeof(T));
            var collection = (IReadOnlyCollection<T>)getSheetTyped.Invoke(_excelService, [_excelTab.SelectedLanguage])!;
            Rows = [.. collection];
        }
    }
}

[AutoConstruct]
public partial class ExcelV2SheetColumn<T> : ColumnString<T> where T : struct
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Excel2Tab _excelTab;
    private readonly ExcelTable<T> _excelTable;
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly PropertyInfo _propertyInfo;
    private readonly ImGuiContextMenuService _imGuiContextMenu;

    public Type RowType => typeof(T);
    public Type ColumnType => _propertyInfo.PropertyType;

    [AutoConstructIgnore]
    public string ColumnTypeName { get; private set; }

    [AutoConstructIgnore]
    public bool IsStringType { get; private set; }

    [AutoConstructIgnore]
    public bool IsNumericType { get; private set; }

    [AutoConstructIgnore]
    public bool IsStructType { get; private set; }

    [AutoConstructIgnore]
    public bool IsIconColumn { get; private set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        AutoLabel = false;
        Label = _propertyInfo.Name;
        ColumnTypeName = ColumnType.ReadableTypeName();

        IsStringType = ColumnType == typeof(ReadOnlySeString);
        IsNumericType = ColumnType.IsNumericType();
        IsStructType = !IsStringType && !ColumnType.IsNumericType() && ColumnType.IsStruct();
        IsIconColumn = Label.Contains("Icon") || Label.Contains("Image");

        Flags = ImGuiTableColumnFlags.WidthFixed;
        Width = true switch
        {
            _ when Label is "RowId" or "SubrowId" => 75,
            _ when IsNumericType => 100,
            _ when IsStringType || IsStructType => 200,
            _ => 100
        };
    }

    public override string ToName(T row)
    {
        var value = _propertyInfo.GetValue(row);
        if (value == null)
            return string.Empty;

        if (ColumnType == typeof(ReadOnlySeString))
            return ((ReadOnlySeString)value).ToString();

        if (ColumnType == typeof(RowRef))
            return ((uint)ColumnType.GetProperty("RowId")?.GetValue(value)!).ToString();

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(RowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            return $"{rowRefType.Name}#{rowRefRowId}";
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(SubrowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            return $"{rowRefType.Name}#{rowRefRowId}";
        }

        return value.ToString() ?? string.Empty;
    }

    public int ToValue(T row)
    {
        return Convert.ToInt32(_propertyInfo.GetValue(row));
    }

    public override int Compare(T lhs, T rhs)
    {
        if (IsStringType)
            return string.Compare(ToName(lhs), ToName(rhs), StringComparison.InvariantCulture);

        if (IsNumericType)
            return ToValue(lhs).CompareTo(ToValue(rhs));

        return 0;
    }

    public bool MatchesSearchTerm(T row)
    {
        return ToName(row).Contains(_excelTab.SearchTerm, StringComparison.InvariantCultureIgnoreCase);
    }

    public override void DrawColumn(T row)
    {
        var value = _propertyInfo.GetValue(row);
        var rowId = (uint)RowType.GetProperty("RowId", BindingFlags.Public | BindingFlags.Instance)!.GetValue(row)!;

        if (value == null)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!_excelTable.IsSubrowType && Label is "RowId" or "SubrowId")
        {
            if (ImGui.Selectable(value.ToString()))
            {
                OpenSheet(RowType.Name, rowId);
            }
            _imGuiContextMenu.Draw($"{RowType.Name}{rowId}RowIdContextMenu", builder =>
            {
                builder.AddCopyRowId(rowId);
            });
            return;
        }

        if (ColumnType == typeof(ReadOnlySeString))
        {
            _debugRenderer.DrawSeString(((ReadOnlySeString)value).AsSpan(), new NodeOptions()
            {
                RenderSeString = false,
                Title = $"{RowType.Name}#{rowId} ({_excelTab.SelectedLanguage})",
                Language = _excelTab.SelectedLanguage,
                // AddressPath = nodeOptions.AddressPath.With(propName.GetHashCode()),
            });
            return;
        }

        if (ColumnType == typeof(RowRef))
        {
            var columnRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            ImGui.Text(columnRowId.ToString());
            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(RowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            var rowRefIsValid = (bool)ColumnType.GetProperty("IsValid")?.GetValue(value)!;
            var text = $"{rowRefType.Name}#{rowRefRowId}";

            if (rowRefIsValid)
            {
                using var color = DebugRenderer.ColorTreeNode.Push(ImGuiCol.Text);

                if (ImGui.Selectable(text))
                    OpenSheet(rowRefType.Name, rowRefRowId);
            }
            else
            {
                using var disabled = ImRaii.Disabled();
                ImGui.Text(text);
            }

            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(SubrowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            var rowRefIsValid = (bool)ColumnType.GetProperty("IsValid")?.GetValue(value)!;
            var text = $"{rowRefType.Name}#{rowRefRowId}";

            if (rowRefIsValid)
            {
                using var color = DebugRenderer.ColorTreeNode.Push(ImGuiCol.Text);

                if (ImGui.Selectable(text))
                    OpenSheet(rowRefType.Name, rowRefRowId);
            }
            else
            {
                using var disabled = ImRaii.Disabled();
                ImGui.Text(text);
            }

            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(Collection<>))
        {
            var count = (int)ColumnType.GetProperty("Count")?.GetValue(value)!;
            using (Color.Grey.Push(ImGuiCol.Text))
                ImGui.Text($"{count} value{(count != 1 ? "s" : "")}"); // TODO: click to open
            return;
        }

        if (IsNumericType)
        {
            _debugRenderer.DrawNumeric(value, ColumnType, new NodeOptions() { IsIconIdField = IsIconColumn, HexOnShift = true });
            return;
        }

        ImGui.Text(value.ToString()); // TODO: invariant culture
    }

    private void OpenSheet(string sheetName, uint rowId)
    {
        if (!_excelTab.TryGetSheetType(sheetName, out var sheetType))
            return;

        var title = $"{sheetName}#{rowId} ({_excelTab.SelectedLanguage})";
        _windowManager.CreateOrOpen(title, () => ActivatorUtilities.CreateInstance<ExcelRowTab>(_serviceProvider, sheetType, rowId, _excelTab.SelectedLanguage, title));
    }
}
