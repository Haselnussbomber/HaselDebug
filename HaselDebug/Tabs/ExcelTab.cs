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

    private readonly TextService TextService;
    private readonly DebugRenderer DebugRenderer;
    private readonly ExcelModule ExcelModule;
    private Addon[] AddonRows;
    private LogMessage[] LogMessageRows;
    private Addon[]? FilteredAddonRows;
    private LogMessage[]? FilteredLogMessageRows;
    private CancellationTokenSource? FilterCTS;
    private string SearchTerm = string.Empty;
    private ClientLanguage SelectedLanguage;

    public ExcelTab(TextService textService, DebugRenderer debugRenderer, ExcelModule excelModule)
    {
        TextService = textService;
        DebugRenderer = debugRenderer;
        ExcelModule = excelModule;

        SelectedLanguage = TextService.ClientLanguage;
        AddonRows = ExcelModule.GetSheet<Addon>(SelectedLanguage.ToLumina()).ToArray();
        LogMessageRows = ExcelModule.GetSheet<LogMessage>(SelectedLanguage.ToLumina()).ToArray();
    }

    public override bool DrawInChild => false;
    public override void Draw()
    {
        using var hostChild = ImRaii.Child("Host", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - LanguageSelectorWidth * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X);
        var listDirty = ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref SearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
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
                        AddonRows = ExcelModule.GetSheet<Addon>(SelectedLanguage.ToLumina()).ToArray();
                        LogMessageRows = ExcelModule.GetSheet<LogMessage>(SelectedLanguage.ToLumina()).ToArray();
                        listDirty |= true;
                    }
                }
            }
        }
        if (listDirty)
        {
            FilterCTS?.Cancel();
            FilterCTS = new();
            Task.Run(() => FilterList(FilterCTS.Token));
        }

        using var tabBar = ImRaii.TabBar("ExcelTabs");
        if (!tabBar) return;

        DrawAddonTab();
        DrawLogMessageTab();
    }

    public void DrawAddonTab()
    {
        var tabTitle = "Addon";

        if (!string.IsNullOrWhiteSpace(SearchTerm) && FilteredAddonRows != null)
        {
            tabTitle = $"{tabTitle} ({FilteredAddonRows.Length})";
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

        ImGuiClip.ClippedDraw(FilteredAddonRows ?? AddonRows, DrawAddonRow, ImGui.GetTextLineHeightWithSpacing());
    }

    public void DrawLogMessageTab()
    {
        var tabTitle = "LogMessage";

        if (!string.IsNullOrWhiteSpace(SearchTerm) && FilteredLogMessageRows != null)
        {
            tabTitle = $"{tabTitle} ({FilteredLogMessageRows.Length})";
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

        ImGuiClip.ClippedDraw(FilteredLogMessageRows ?? LogMessageRows, DrawLogMessageRow, ImGui.GetTextLineHeightWithSpacing());
    }

    private void FilterList(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            FilteredAddonRows = null;
            FilteredLogMessageRows = null;
            return;
        }

        var addonList = new List<Addon>();

        for (var i = 0; i < AddonRows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = AddonRows[i];
            if (row.RowId.ToString().Contains(SearchTerm)
             || row.Text.ToString().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
             || row.Text.ExtractText().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                addonList.Add(row);
            }
        }

        FilteredAddonRows = addonList.ToArray();

        var logMessageList = new List<LogMessage>();

        for (var i = 0; i < LogMessageRows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = LogMessageRows[i];
            if (row.RowId.ToString().Contains(SearchTerm)
             || row.Text.ToString().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
             || row.Text.ExtractText().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                logMessageList.Add(row);
            }
        }

        FilteredLogMessageRows = logMessageList.ToArray();
    }

    private void DrawAddonRow(Addon row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Text
        DebugRenderer.DrawSeStringSelectable(row.Text.AsSpan(), new NodeOptions()
        {
            AddressPath = new AddressPath((nint)row.RowId),
            RenderSeString = false,
            Title = $"Addon#{row.RowId} ({SelectedLanguage})",
            Language = SelectedLanguage
        });
    }

    private void DrawLogMessageRow(LogMessage row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Text
        DebugRenderer.DrawSeStringSelectable(row.Text.AsSpan(), new NodeOptions()
        {
            AddressPath = new AddressPath((nint)row.RowId),
            RenderSeString = false,
            Title = $"LogMessage#{row.RowId} ({SelectedLanguage})",
            Language = SelectedLanguage
        });
    }
}
