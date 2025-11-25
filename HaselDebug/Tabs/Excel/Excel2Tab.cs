using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HaselDebug.Abstracts;
using HaselDebug.Config;
using HaselDebug.Interfaces;
using HaselDebug.Windows;
using Lumina.Data.Files.Excel;

namespace HaselDebug.Tabs.Excel;

#pragma warning disable PendingExcelSchema

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
    private bool _searchMacroString = true;
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

        try
        {
            _allSheetNames = _dataManager.GameData.GetFile<ExcelListFile>("exd/root.exl") is { } excelListFile
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

    public void ChangeSheet(string sheetName)
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

    private void DrawGlobalSearch()
    {
        ImGui.Separator();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Search in all sheets:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(300);
        using (ImRaii.Disabled(_isSearching))
            ImGui.InputTextWithHint("##GlobalSearch", "Enter search term...", ref _globalSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

        ImGui.SameLine();
        ImGui.Checkbox("MacroString", ref _searchMacroString);

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
        var searchTerm = new ParsedSearchTerm(_globalSearchTerm, _searchMacroString);
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

                        if (searchTerm.IsMatch(prop.PropertyType, value, out var propValue, out var idx))
                        {
                            results.Add(new GlobalSearchResult(
                                true,
                                "Typed",
                                sheetName,
                                $"{rowId}.{subrowId}",
                                index,
                                prop.Name + (idx == -1 ? string.Empty : $"[{idx}]"),
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

                    if (searchTerm.IsMatch(prop.PropertyType, value, out var propValue, out var idx))
                    {
                        results.Add(new GlobalSearchResult(
                            false,
                            "Typed",
                            sheetName,
                            rowId.ToString(),
                            index,
                            prop.Name + (idx == -1 ? string.Empty : $"[{idx}]"),
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
