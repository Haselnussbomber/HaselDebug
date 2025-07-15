using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ExcelTab : DebugTab
{
    private const int LanguageSelectorWidth = 90;

    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly IDataManager _dataManager;
    private readonly DebugRenderer _debugRenderer;

    private CancellationTokenSource? _filterCTS;

    private IExcelSheetTab[] _excelTabs;
    private bool _isInitialized;

    public string SearchTerm { get; private set; } = string.Empty;
    public ClientLanguage SelectedLanguage { get; private set; }

    private void Initialize()
    {
        SelectedLanguage = _languageProvider.ClientLanguage;

        _excelTabs = [
            new ExcelSheetTab<Addon>(this, _dataManager) {
                Columns = [
                    new ExcelSheetStringColumn<Addon>(this, _debugRenderer, "Text", (row) => row.Text),
                ]
            },
            new ExcelSheetTab<AddonTransient>(this, _dataManager) {
                Columns = [
                    new ExcelSheetStringColumn<AddonTransient>(this, _debugRenderer, "Unknown0", (row) => row.Unknown0),
                ]
            },
            new ExcelSheetTab<Lobby>(this, _dataManager) {
                Columns = [
                    new ExcelSheetStringColumn<Lobby>(this, _debugRenderer, "Text", (row) => row.Text),
                ]
            },
            new ExcelSheetTab<LogMessage>(this, _dataManager) {
                Columns = [
                    new ExcelSheetStringColumn<LogMessage>(this, _debugRenderer, "Text", (row) => row.Text),
                ]
            },
            new ExcelSheetTab<LogKind>(this, _dataManager) {
                Columns = [
                    new ExcelSheetStringColumn<LogKind>(this, _debugRenderer, "Format", (row) => row.Format),
                ]
            },
        ];
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

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - LanguageSelectorWidth * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X);
        var searchTerm = SearchTerm;
        var listDirty = false;
        if (ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll))
        {
            SearchTerm = searchTerm;
            listDirty |= true;
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(LanguageSelectorWidth * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language", SelectedLanguage.ToString() ?? "Language..."))
        {
            if (dropdown)
            {
                var values = Enum.GetValues<ClientLanguage>().OrderBy((ClientLanguage lang) => lang.ToString());
                foreach (var value in values)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == SelectedLanguage))
                    {
                        SelectedLanguage = value;
                        foreach (var tab in _excelTabs)
                            tab.ReloadSheet();
                        listDirty |= true;
                    }
                }
            }
        }

        if (listDirty)
        {
            _filterCTS?.Cancel();
            _filterCTS = new();
            Task.Run(() =>
            {
                foreach (var tab in _excelTabs)
                    tab.ReloadFilteredRows(_filterCTS.Token);
            });
        }

        using var tabBar = ImRaii.TabBar("ExcelTabs");
        if (!tabBar) return;

        foreach (var tab in _excelTabs)
            tab.Draw();
    }
}

public interface IExcelSheetTab
{
    void Draw();
    void ReloadFilteredRows(CancellationToken cancellationToken);
    void ReloadSheet();
}

public class ExcelSheetTab<T> : IExcelSheetTab where T : struct, IExcelRow<T>
{
    private readonly ExcelTab _excelTab;
    private readonly IDataManager _dataManager;

    private readonly string _sheetName = typeof(T).Name;
    private T[] _rows = null!;
    private T[]? _filtereRows;

    public ExcelSheetColumn<T>[] Columns { get; set; } = [];

    public ExcelSheetTab(ExcelTab excelTab, IDataManager dataManager)
    {
        _excelTab = excelTab;
        _dataManager = dataManager;
        ReloadSheet();
    }

    public void ReloadSheet()
    {
        _rows = [.. _dataManager.Excel.GetSheet<T>(_excelTab.SelectedLanguage.ToLumina())];
    }

    public void ReloadFilteredRows(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_excelTab.SearchTerm))
        {
            _filtereRows = null;
            return;
        }

        var rows = new List<T>();

        for (var i = 0; i < _rows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = _rows[i];

            if (row.RowId.ToString().Contains(_excelTab.SearchTerm) || Columns.Any(col => col.MatchesSearchTerm?.Invoke(row) == true))
                rows.Add(row);
        }

        _filtereRows = [.. rows];
    }

    public void Draw()
    {
        var tabTitle = _sheetName;

        if (!string.IsNullOrWhiteSpace(_excelTab.SearchTerm) && _filtereRows != null)
        {
            tabTitle = $"{tabTitle} ({_filtereRows.Length})";
        }

        using var tab = ImRaii.TabItem(tabTitle + $"###{_sheetName}Tab");
        if (!tab) return;

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        using var table = ImRaii.Table("RowTable", 1 + Columns.Length, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);

        foreach (var column in Columns)
            ImGui.TableSetupColumn(column.Name, column.TableColumnFlags, column.TableColumnWidth);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        ImGuiClip.ClippedDraw(_filtereRows ?? _rows, DrawRow, ImGui.GetTextLineHeightWithSpacing());
    }

    private void DrawRow(T row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        foreach (var column in Columns)
        {
            ImGui.TableNextColumn();
            column.Draw?.Invoke(row);
        }
    }
}

public class ExcelSheetColumn<T>() where T : struct, IExcelRow<T>
{
    public string Name { get; init; } = string.Empty;
    public Action<T>? Draw { get; init; }
    public Func<T, bool>? MatchesSearchTerm { get; init; }
    public ImGuiTableColumnFlags TableColumnFlags { get; init; } = ImGuiTableColumnFlags.WidthStretch;
    public float TableColumnWidth { get; init; }
}

public class ExcelSheetStringColumn<T> : ExcelSheetColumn<T> where T : struct, IExcelRow<T>
{
    public ExcelSheetStringColumn(ExcelTab excelTab, DebugRenderer debugRenderer, string name, Func<T, ReadOnlySeString> stringGetter)
    {
        Name = name;

        MatchesSearchTerm = (row) =>
        {
            return stringGetter(row).ToString().Contains(excelTab.SearchTerm, StringComparison.InvariantCultureIgnoreCase);
        };

        Draw = (row) =>
        {
            debugRenderer.DrawSeString(stringGetter(row).AsSpan(), new NodeOptions()
            {
                AddressPath = new AddressPath((nint)row.RowId),
                RenderSeString = false,
                Title = $"{row.GetType().Name}#{row.RowId} ({excelTab.SelectedLanguage})",
                Language = excelTab.SelectedLanguage
            });
        };
    }
}
