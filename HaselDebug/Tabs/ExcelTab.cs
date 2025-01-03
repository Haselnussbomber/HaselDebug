using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class ExcelTab : DebugTab
{
    private const int LanguageSelectorWidth = 90;

    private readonly TextService _textService;
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelModule _excelModule;
    private Addon[] _addonRows;
    private LogMessage[] _logMessageRows;
    private Addon[]? _filteredAddonRows;
    private LogMessage[]? _filteredLogMessageRows;
    private CancellationTokenSource? _filterCTS;
    private string _searchTerm = string.Empty;
    private ClientLanguage _selectedLanguage;

    public ExcelTab(TextService textService, DebugRenderer debugRenderer, ExcelModule excelModule)
    {
        _textService = textService;
        _debugRenderer = debugRenderer;
        _excelModule = excelModule;

        _selectedLanguage = _textService.ClientLanguage;
        _addonRows = _excelModule.GetSheet<Addon>(_selectedLanguage.ToLumina()).ToArray();
        _logMessageRows = _excelModule.GetSheet<LogMessage>(_selectedLanguage.ToLumina()).ToArray();
    }

    public override bool DrawInChild => false;
    public override void Draw()
    {
        using var hostChild = ImRaii.Child("Host", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - LanguageSelectorWidth * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X);
        var listDirty = ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(LanguageSelectorWidth * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language", _selectedLanguage.ToString() ?? "Language..."))
        {
            if (dropdown)
            {
                var values = Enum.GetValues<ClientLanguage>().OrderBy((ClientLanguage lang) => lang.ToString());
                foreach (var value in values)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == _selectedLanguage))
                    {
                        _selectedLanguage = value;
                        _addonRows = _excelModule.GetSheet<Addon>(_selectedLanguage.ToLumina()).ToArray();
                        _logMessageRows = _excelModule.GetSheet<LogMessage>(_selectedLanguage.ToLumina()).ToArray();
                        listDirty |= true;
                    }
                }
            }
        }
        if (listDirty)
        {
            _filterCTS?.Cancel();
            _filterCTS = new();
            Task.Run(() => FilterList(_filterCTS.Token));
        }

        using var tabBar = ImRaii.TabBar("ExcelTabs");
        if (!tabBar) return;

        DrawAddonTab();
        DrawLogMessageTab();
    }

    public void DrawAddonTab()
    {
        var tabTitle = "Addon";

        if (!string.IsNullOrWhiteSpace(_searchTerm) && _filteredAddonRows != null)
        {
            tabTitle = $"{tabTitle} ({_filteredAddonRows.Length})";
        }

        using var tab = ImRaii.TabItem(tabTitle + "###AddonTab");
        if (!tab) return;

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        using var table = ImRaii.Table("RowTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        ImGuiClip.ClippedDraw(_filteredAddonRows ?? _addonRows, DrawAddonRow, ImGui.GetTextLineHeightWithSpacing());
    }

    public void DrawLogMessageTab()
    {
        var tabTitle = "LogMessage";

        if (!string.IsNullOrWhiteSpace(_searchTerm) && _filteredLogMessageRows != null)
        {
            tabTitle = $"{tabTitle} ({_filteredLogMessageRows.Length})";
        }

        using var tab = ImRaii.TabItem(tabTitle + "###LogMessageTab");
        if (!tab) return;

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        using var table = ImRaii.Table("RowTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        ImGuiClip.ClippedDraw(_filteredLogMessageRows ?? _logMessageRows, DrawLogMessageRow, ImGui.GetTextLineHeightWithSpacing());
    }

    private void FilterList(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            _filteredAddonRows = null;
            _filteredLogMessageRows = null;
            return;
        }

        var addonList = new List<Addon>();

        for (var i = 0; i < _addonRows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = _addonRows[i];
            if (row.RowId.ToString().Contains(_searchTerm)
             || row.Text.ToString().Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase)
             || row.Text.ExtractText().Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                addonList.Add(row);
            }
        }

        _filteredAddonRows = addonList.ToArray();

        var logMessageList = new List<LogMessage>();

        for (var i = 0; i < _logMessageRows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = _logMessageRows[i];
            if (row.RowId.ToString().Contains(_searchTerm)
             || row.Text.ToString().Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase)
             || row.Text.ExtractText().Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                logMessageList.Add(row);
            }
        }

        _filteredLogMessageRows = logMessageList.ToArray();
    }

    private void DrawAddonRow(Addon row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Text
        _debugRenderer.DrawSeString(row.Text.AsSpan(), new NodeOptions()
        {
            AddressPath = new AddressPath((nint)row.RowId),
            RenderSeString = false,
            Title = $"Addon#{row.RowId} ({_selectedLanguage})",
            Language = _selectedLanguage
        });
    }

    private void DrawLogMessageRow(LogMessage row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Text
        _debugRenderer.DrawSeString(row.Text.AsSpan(), new NodeOptions()
        {
            AddressPath = new AddressPath((nint)row.RowId),
            RenderSeString = false,
            Title = $"LogMessage#{row.RowId} ({_selectedLanguage})",
            Language = _selectedLanguage
        });
    }
}
