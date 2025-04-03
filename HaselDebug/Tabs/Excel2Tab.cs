using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Extensions.Reflection;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class Excel2Tab : DebugTab
{
    public const int MaxColumns = 60;
    private const int LanguageSelectorWidth = 90;

    private readonly ExcelService _excelService;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly IDataManager _dataManager;
    private readonly DebugRenderer _debugRenderer;

    private Dictionary<string, Type> _sheetTypes;
    private IExcelV2SheetWrapper? _sheetWrapper;
    private IExcelV2SheetWrapper? _nextSheetWrapper;
    private string _sheetNameSearchTerm = string.Empty;

    public override string Title => "Excel (v2)";

    public string SearchTerm { get; private set; } = string.Empty;
    public ClientLanguage SelectedLanguage { get; private set; }

    [AutoPostConstruct]
    public void Initialize()
    {
        SelectedLanguage = _languageProvider.ClientLanguage;

        _sheetTypes = typeof(Lumina.Excel.Sheets.Achievement).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Lumina.Excel.Sheets" && !string.IsNullOrEmpty(type.GetCustomAttribute<SheetAttribute>()?.Name))
            .ToDictionary(type => type.GetCustomAttribute<SheetAttribute>()!.Name!);
    }

    public override bool DrawInChild => false;
    public override void Draw()
    {
        using var hostChild = ImRaii.Child("Host", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);
        if (!hostChild) return;

        ImGui.TextUnformatted("Work in progress!");

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

        DrawSheetList();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var innerChild = ImRaii.Child("InnerHost", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);
        if (!innerChild) return;

        if (_sheetWrapper == null)
        {
            ImGui.TextUnformatted("No sheet selected.");
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

        using var table = ImRaii.Table("SheetTable", 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(1, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        foreach (var sheetName in _sheetTypes.Keys.OrderBy(sheetName => sheetName))
        {
            if (hasSearchTerm && !sheetName.Contains(_sheetNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Name
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

        _nextSheetWrapper = (IExcelV2SheetWrapper)Activator.CreateInstance(
            typeof(ExcelV2SheetWrapper<>).MakeGenericType(sheetType),
            this, _debugRenderer, _dataManager, _languageProvider, _excelService)!;
    }

    public bool TryGetSheetType(string sheetName, [NotNullWhen(returnValue: true)] out Type? sheetType)
        => _sheetTypes.TryGetValue(sheetName, out sheetType);
}

public interface IExcelV2SheetWrapper
{
    string SheetName { get; }
    ClientLanguage Language { get; }
    void Draw();
    void ReloadSheet();
}

public class ExcelV2SheetWrapper<T> : IExcelV2SheetWrapper where T : struct
{
    private readonly ExcelTable<T> _table;
    private readonly Excel2Tab _excelTab;

    public List<ExcelV2SheetColumn<T>> Columns { get; set; } = [];
    public string SheetName { get; } = typeof(T).Name;
    public ClientLanguage Language => _excelTab.SelectedLanguage;

    public ExcelV2SheetWrapper(Excel2Tab excelTab, DebugRenderer debugRenderer, IDataManager dataManager, LanguageProvider languageProvider, ExcelService excelService)
    {
        _table = new ExcelTable<T>(languageProvider, excelTab, excelService, debugRenderer);

        ReloadSheet();
        _excelTab = excelTab;
    }

    public void ReloadSheet()
    {
        _table.RowsLoaded = false;
    }

    public void Draw()
    {
        ImGui.TextUnformatted(SheetName);
        ImGui.SameLine();

        var count = (_table.FilteredRows ?? _table.Rows).Count;
        ImGui.TextUnformatted($"{count} row{(count != 1 ? "s" : "")}");
        ImGui.TextUnformatted($"IsSubrowType: {_table.IsSubrowType}");

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
    private readonly Excel2Tab _excelTab;
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;

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

        var columnType = typeof(ExcelV2SheetColumn<>).MakeGenericType(typeof(T));
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var column = (ExcelV2SheetColumn<T>)Activator.CreateInstance(columnType, _excelTab, this, _debugRenderer, property)!;

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

public class ExcelV2SheetColumn<T> : ColumnString<T> where T : struct
{
    private readonly Excel2Tab _excelTab;
    private readonly ExcelTable<T> _excelTable;
    private readonly DebugRenderer _debugRenderer;
    private readonly PropertyInfo _propertyInfo;

    public Type RowType => typeof(T);
    public Type ColumnType => _propertyInfo.PropertyType;
    public string ColumnTypeName { get; }

    public bool IsStringType { get; }
    public bool IsNumericType { get; }
    public bool IsStructType { get; }
    public bool IsIconColumn { get; }

    public ExcelV2SheetColumn(Excel2Tab excelTab, ExcelTable<T> excelTable, DebugRenderer debugRenderer, PropertyInfo columnPropertyInfo)
    {
        _excelTab = excelTab;
        _excelTable = excelTable;
        _debugRenderer = debugRenderer;
        _propertyInfo = columnPropertyInfo;

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
            ImGui.TextUnformatted("null");
            return;
        }

        if (!_excelTable.IsSubrowType && Label is "RowId" or "SubrowId")
        {
            if (ImGui.Selectable(value.ToString()))
            {
                OpenSheet(RowType.Name, rowId);
            }
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
            ImGui.TextUnformatted(columnRowId.ToString());
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
                ImGui.TextUnformatted(text);
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
                ImGui.TextUnformatted(text);
            }

            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(Collection<>))
        {
            var count = (int)ColumnType.GetProperty("Count")?.GetValue(value)!;
            using (Color.Grey.Push(ImGuiCol.Text))
                ImGui.TextUnformatted($"{count} value{(count != 1 ? "s" : "")}"); // TODO: click to open
            return;
        }

        if (IsNumericType)
        {
            _debugRenderer.DrawNumeric(value, ColumnType, new NodeOptions() { IsIconIdField = IsIconColumn, HexOnShift = true });
            return;
        }

        ImGui.TextUnformatted(value.ToString()); // TODO: invariant culture
    }

    private void OpenSheet(string sheetName, uint rowId)
    {
        if (!_excelTab.TryGetSheetType(sheetName, out var sheetType))
            return;

        var windowManager = Service.Get<WindowManager>();
        var title = $"{sheetName}#{rowId} ({_excelTab.SelectedLanguage})";
        windowManager.CreateOrOpen(title, () => new ExcelRowTab(windowManager, Service.Get<TextService>(), Service.Get<LanguageProvider>(), _debugRenderer, sheetType, rowId, _excelTab.SelectedLanguage, title));
    }
}
