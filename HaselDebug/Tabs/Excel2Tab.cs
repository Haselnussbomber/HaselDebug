using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Abstracts;
using HaselDebug.Config;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using Lumina.Data.Structs.Excel;

namespace HaselDebug.Tabs;

#pragma warning disable PendingExcelSchema

public record GlobalSearchResult(bool IsSubrowSheet, string SheetType, string SheetName, string RowId, int ColumnIndex, string ColumnName, string Value);

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
    private readonly PluginConfig _pluginConfig;

    private Dictionary<string, Type> _sheetTypes;
    private HashSet<string> _allSheetNames;
    private IExcelV2SheetWrapper? _sheetWrapper;
    private IExcelV2SheetWrapper? _nextSheetWrapper;
    private string _sheetNameSearchTerm = string.Empty;
    private bool _useExperimentalSheets = true;
    private bool _showRawSheets = false;
    private bool _isInitialized;

    private string _globalSearchTerm = string.Empty;
    private List<GlobalSearchResult> _globalSearchResults = [];
    private bool _isSearching = false;
    private bool _openResultsWindowOnNextFrame = false;
    private CancellationTokenSource? _searchCts;

    public override string Title => "Excel (v2)";

    public string SearchTerm { get; private set; } = string.Empty;
    public ClientLanguage SelectedLanguage { get; private set; }

    private void Initialize()
    {
        SelectedLanguage = _languageProvider.ClientLanguage;
        _showRawSheets = _pluginConfig.Excel2Tab_ShowRawSheets;
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
            _allSheetNames = excelListFile != null
                ? [.. excelListFile.ExdMap.Keys]
                : [.. _sheetTypes.Keys];
        }
        catch
        {
            _allSheetNames = [.. _sheetTypes.Keys];
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

        // Open results window
        if (_openResultsWindowOnNextFrame)
        {
            _openResultsWindowOnNextFrame = false;
            OpenSearchResultsWindow();
        }

        if (_nextSheetWrapper != null)
        {
            _sheetWrapper = _nextSheetWrapper;
            _nextSheetWrapper = null;
        }

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
        if (ImGui.Checkbox("Show Raw Sheets", ref _showRawSheets))
        {
            _pluginConfig.Excel2Tab_ShowRawSheets = _showRawSheets;
            _pluginConfig.Save();
        }

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

            // Skip raw sheets
            if (!hasType && !_showRawSheets)
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

    /// <summary>
    /// Changes the currently displayed sheet. Determines if the sheet is typed or raw,
    /// then creates the appropriate wrapper to display the data.
    /// </summary>
    private void ChangeSheet(string sheetName)
    {
        // Handle typed sheets with type definitions
        if (TryGetSheetType(sheetName, out var sheetType))
        {
            // For typed sheets, use ExcelV2SheetWrapper
            _nextSheetWrapper = (IExcelV2SheetWrapper)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                typeof(ExcelV2SheetWrapper<>).MakeGenericType(sheetType),
                this);
        }
        else
        {
            // For raw sheets, use RawSheetWrapper
            _nextSheetWrapper = ActivatorUtilities.CreateInstance<RawSheetWrapper>(
                _serviceProvider,
                this,
                sheetName);
        }
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
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Search in all sheets:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(300);
        using (ImRaii.Disabled(_isSearching))
            ImGui.InputTextWithHint("##GlobalSearch", "Enter search term...", ref _globalSearchTerm, 256);

        ImGui.SameLine();
        if (_isSearching && ImGui.Button("Cancel"))
        {
            CancelGlobalSearch();
        }
        else if (!_isSearching && ImGui.Button("Search"))
        {
            StartGlobalSearch();
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

    // Opens a new window to display search results
    private void OpenSearchResultsWindow()
    {
        var windowTitle = $"Excel Search Results for \"{_globalSearchTerm}\" ({_globalSearchResults.Count} results)";

        _windowManager.CreateOrOpen(windowTitle, () =>
            ActivatorUtilities.CreateInstance<ExcelSearchResultsWindow>(
                _serviceProvider,
                this,
                _globalSearchTerm,
                new List<GlobalSearchResult>(_globalSearchResults),
                windowTitle
            ));
    }

    private void CancelGlobalSearch()
    {
        _searchCts?.Cancel();
        _searchCts = null;
        _isSearching = false;
        _globalSearchResults.Clear();
    }

    // Initiates a global search across all Excel sheets (typed and raw)
    // Search runs asynchronously to avoid blocking the UI
    private void StartGlobalSearch()
    {
        if (string.IsNullOrWhiteSpace(_globalSearchTerm))
            return;

        _logger.LogInformation("[Excel2Tab] Starting global search for: {SearchTerm}", _globalSearchTerm);

        CancelGlobalSearch();

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        var searchTerm = new ParsedSearchTerm(_globalSearchTerm);
        _isSearching = true;

        _logger.LogInformation("[Excel2Tab] Searching {SheetCount} sheets", _allSheetNames.Count);

        Task.Run(() =>
        {
            try
            {
                var dict = new ConcurrentDictionary<string, List<GlobalSearchResult>>();

                Parallel.ForEach(_allSheetNames, new ParallelOptions() { CancellationToken = token }, (sheetName) =>
                {
                    try
                    {
                        var results = new List<GlobalSearchResult>();

                        if (TryGetSheetType(sheetName, out var sheetType))
                            SearchSheet(sheetType, sheetName, searchTerm, results, token);
                        else
                            SearchSheetRaw(sheetName, searchTerm, results, token);

                        dict.TryAdd(sheetName, results);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[Excel2Tab] Failed to search sheet: {SheetName}", ex.Message);
                    }
                });

                _logger.LogInformation("[Excel2Tab] Searched {SheetsSearched} sheets, found {ResultCount} results", dict.Count, dict.Values.Sum(l => l.Count));

                if (!token.IsCancellationRequested)
                {
                    _globalSearchResults = [.. dict.OrderBy(kv => kv.Key).SelectMany(kv => kv.Value)];
                    _openResultsWindowOnNextFrame = true;
                }
            }
            finally
            {
                _isSearching = false;
            }
        }, token);
    }

    // Searches a typed sheet using reflection to access all properties
    private void SearchSheet(Type sheetType, string sheetName, ParsedSearchTerm searchTerm, List<GlobalSearchResult> results, CancellationToken token)
    {
        var properties = sheetType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var isSubrowType = sheetType.IsAssignableTo(typeof(IExcelSubrow<>).MakeGenericType(sheetType));
        if (isSubrowType)
        {
            var getSheetMethodInfo = typeof(ExcelService)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(mi => mi.Name == "GetSubrowSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1);

            var sheet = getSheetMethodInfo
                .MakeGenericMethod(sheetType)
                .Invoke(_excelService, [SelectedLanguage]);
            if (sheet == null)
                return;

            foreach (var row in (IEnumerable)sheet)
            {
                if (token.IsCancellationRequested)
                    break;

                var rowProperties = row.GetType() // SubrowCollection<T>
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var rowIdProp = rowProperties.FirstOrDefault(p => p.Name == "RowId");
                var rowId = rowIdProp != null ? (uint)rowIdProp.GetValue(row)! : 0u;

                foreach (var subrow in (IEnumerable)row)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var subrowIdProp = properties.FirstOrDefault(p => p.Name == "SubrowId");
                    var subrowId = subrowIdProp != null ? (ushort)subrowIdProp.GetValue(subrow)! : 0u;

                    foreach (var (index, prop) in properties.Index())
                    {
                        var value = prop.GetValue(subrow);
                        if (value == null)
                            continue;

                        if (searchTerm.IsMatch(prop, value, out var propValue))
                        {
                            results.Add(new GlobalSearchResult(
                                true,
                                "Typed",
                                sheetName,
                                $"{rowId}.{subrowId}",
                                index,
                                prop.Name,
                                propValue
                            ));
                        }
                    }
                }
            }
        }
        else
        {
            var getSheetMethodInfo = typeof(ExcelService)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(mi => mi.Name == "GetSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1);

            var sheet = getSheetMethodInfo
                .MakeGenericMethod(sheetType)
                .Invoke(_excelService, [SelectedLanguage]);
            if (sheet == null)
                return;

            foreach (var row in (IEnumerable)sheet)
            {
                if (token.IsCancellationRequested)
                    break;

                var rowIdProp = properties.FirstOrDefault(p => p.Name == "RowId");
                var rowId = rowIdProp != null ? (uint)rowIdProp.GetValue(row)! : 0u;

                foreach (var (index, prop) in properties.Index())
                {
                    var value = prop.GetValue(row);
                    if (value == null)
                        continue;

                    if (searchTerm.IsMatch(prop, value, out var propValue))
                    {
                        results.Add(new GlobalSearchResult(
                            false,
                            "Typed",
                            sheetName,
                            rowId.ToString(),
                            index,
                            prop.Name,
                            propValue
                        ));
                    }
                }
            }
        }
    }

    private void SearchSheetRaw(string sheetName, ParsedSearchTerm searchTerm, List<GlobalSearchResult> results, CancellationToken token)
    {
        var sheet = _excelService.GetSheet<RawRow>(sheetName, SelectedLanguage);
        if (sheet == null)
            return;

        foreach (var row in sheet)
        {
            if (token.IsCancellationRequested)
                break;

            for (var i = 0; i < row.Columns.Count; i++)
            {
                if (searchTerm.IsMatch(row, i, out var columnValue))
                {
                    results.Add(new GlobalSearchResult(
                        false,
                        "Raw",
                        sheetName,
                        row.RowId.ToString(),
                        i,
                        $"Column{i}",
                        columnValue
                    ));
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

/// <summary>
/// Displays raw sheet data using binary RawRow API
/// </summary>
[AutoConstruct]
public partial class RawSheetWrapper : IExcelV2SheetWrapper
{
    private readonly Excel2Tab _excelTab;
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;

    private List<RawRow> _rows = [];
    private List<RawRow> _filteredRows = [];
    private IReadOnlyList<ExcelColumnDefinition> _columns = [];

    public string SheetName { get; init; } = string.Empty;

    public ClientLanguage Language => _excelTab.SelectedLanguage;

    /// <summary>
    /// Loads all rows from the raw sheet and detects the number of columns using trial and error
    /// </summary>
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

    /// <summary>Render raw sheet data as a table with a RowId column and dynamic data columns.</summary>
    public void Draw()
    {
        // Only fetch sheet data when first drawn (not when ChangeSheet is called)
        if (_rows.Count == 0)
        {
            ReloadSheet();
        }

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

public readonly struct ParsedSearchTerm
{
    public ParsedSearchTerm(string searchTerm)
    {
        String = searchTerm;

        // --- Constructor Body (Parsing Logic) ---
        IsBool = bool.TryParse(searchTerm, out Bool);
        IsSByte = sbyte.TryParse(searchTerm, out SByte);
        IsByte = byte.TryParse(searchTerm, out Byte);
        IsShort = short.TryParse(searchTerm, out Short);
        IsUShort = ushort.TryParse(searchTerm, out UShort);
        IsInt = int.TryParse(searchTerm, out Int);
        IsUInt = uint.TryParse(searchTerm, out UInt);
        IsLong = long.TryParse(searchTerm, out Long);
        IsULong = ulong.TryParse(searchTerm, out ULong);
        IsFloat = float.TryParse(searchTerm, out Float);
    }

    public readonly string String;

    // --- Field Definitions (Flag and Value) ---

    // Boolean
    public readonly bool IsBool;
    public readonly bool Bool;

    // Signed Integers
    public readonly bool IsSByte;
    public readonly sbyte SByte;
    public readonly bool IsShort;
    public readonly short Short;
    public readonly bool IsInt;
    public readonly int Int;
    public readonly bool IsLong;
    public readonly long Long;

    // Unsigned Integers
    public readonly bool IsByte;
    public readonly byte Byte;
    public readonly bool IsUShort;
    public readonly ushort UShort;
    public readonly bool IsUInt;
    public readonly uint UInt;
    public readonly bool IsULong;
    public readonly ulong ULong;

    // Floating Point Numbers
    public readonly bool IsFloat;
    public readonly float Float;

    public bool IsMatch(PropertyInfo prop, object? value, [NotNullWhen(returnValue: true)] out string? columnValue)
    {
        if (prop.PropertyType == typeof(ReadOnlySeString)
            && value is ReadOnlySeString stringValue
            && stringValue.ToString("m") is { } macroString
            && macroString.Contains(String, StringComparison.InvariantCultureIgnoreCase))
        {
            columnValue = macroString;
            return true;
        }

        if (prop.PropertyType == typeof(bool)
            && IsBool
            && value is bool boolValue
            && boolValue == Bool)
        {
            columnValue = boolValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(sbyte)
            && IsSByte
            && value is sbyte sbyteValue
            && sbyteValue == SByte)
        {
            columnValue = sbyteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(short)
            && IsShort
            && value is short shortValue
            && shortValue == Short)
        {
            columnValue = shortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(int)
            && IsInt
            && value is int intValue
            && intValue == Int)
        {
            columnValue = intValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(long)
            && IsLong
            && value is long longValue
            && longValue == Long)
        {
            columnValue = longValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(byte)
            && IsByte
            && value is byte byteValue
            && byteValue == Byte)
        {
            columnValue = byteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(ushort)
            && IsUShort
            && value is ushort ushortValue
            && ushortValue == UShort)
        {
            columnValue = ushortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(uint)
            && IsUInt
            && value is uint uintValue
            && uintValue == UInt)
        {
            columnValue = uintValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(ulong)
            && IsULong
            && value is ulong ulongValue
            && ulongValue == ULong)
        {
            columnValue = ulongValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(float)
            && IsFloat
            && value is float floatValue
            && floatValue == Float)
        {
            columnValue = floatValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }


        if (prop.PropertyType.IsGenericType
            && IsUInt
            && prop.PropertyType.GetGenericTypeDefinition() is { } genericTypeDefinition
            && genericTypeDefinition == typeof(RowRef<>)
            && prop.PropertyType.GetProperty("RowId", BindingFlags.Public | BindingFlags.Instance) is { } rowIdProp
            && rowIdProp.GetValue(value) is uint rowIdValue
            && rowIdValue == UInt)
        {
            columnValue = rowIdValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        columnValue = null;
        return false;
    }

    public bool IsMatch(RawRow row, int columnIndex, [NotNullWhen(returnValue: true)] out string? columnValue)
    {
        var column = row.Columns[columnIndex];

        if (column.Type == ExcelColumnDataType.String
            && row.ReadStringColumn(columnIndex) is { } stringValue
            && stringValue.ToString("m") is { } macroString
            && macroString.Contains(String, StringComparison.InvariantCultureIgnoreCase))
        {
            columnValue = macroString;
            return true;
        }

        if (column.Type == ExcelColumnDataType.Bool
            && IsBool
            && row.ReadBoolColumn(columnIndex) is { } boolValue
            && boolValue == Bool)
        {
            columnValue = boolValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int8
            && IsSByte
            && row.ReadInt8Column(columnIndex) is { } sbyteValue
            && sbyteValue == SByte)
        {
            columnValue = sbyteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int16
            && IsShort
            && row.ReadInt16Column(columnIndex) is { } shortValue
            && shortValue == Short)
        {
            columnValue = shortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int32
            && IsInt
            && row.ReadInt32Column(columnIndex) is { } intValue
            && intValue == Int)
        {
            columnValue = intValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int64
            && IsLong
            && row.ReadInt64Column(columnIndex) is { } longValue
            && longValue == Long)
        {
            columnValue = longValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt8
            && IsByte
            && row.ReadUInt8Column(columnIndex) is { } byteValue
            && byteValue == Byte)
        {
            columnValue = byteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt16
            && IsUShort
            && row.ReadUInt16Column(columnIndex) is { } ushortValue
            && ushortValue == UShort)
        {
            columnValue = ushortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt32
            && IsUInt
            && row.ReadUInt32Column(columnIndex) is { } uintValue
            && uintValue == UInt)
        {
            columnValue = uintValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt64
            && IsULong
            && row.ReadUInt64Column(columnIndex) is { } ulongValue
            && ulongValue == ULong)
        {
            columnValue = ulongValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Float32
            && IsFloat
            && row.ReadFloat32Column(columnIndex) is { } floatValue
            && floatValue == Float)
        {
            columnValue = floatValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type
            is ExcelColumnDataType.PackedBool0
            or ExcelColumnDataType.PackedBool1
            or ExcelColumnDataType.PackedBool2
            or ExcelColumnDataType.PackedBool3
            or ExcelColumnDataType.PackedBool4
            or ExcelColumnDataType.PackedBool5
            or ExcelColumnDataType.PackedBool6
            or ExcelColumnDataType.PackedBool7
            && IsBool
            && row.ReadPackedBoolColumn(columnIndex) is { } packedBoolValue
            && packedBoolValue == Bool)
        {
            columnValue = packedBoolValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        columnValue = null;
        return false;
    }
}
