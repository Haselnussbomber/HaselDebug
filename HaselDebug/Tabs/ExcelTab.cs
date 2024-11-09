using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using Dalamud.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using HaselCommon.Extensions.Reflection;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using Microsoft.Extensions.Logging;
using SheetColumn = (string Name, System.Type Type);
using SheetName = (int Id, string Name);

namespace HaselDebug.Tabs;

public class ExcelTab : DebugTab
{
    private readonly ILogger<ExcelTab> logger;
    private readonly TextService textService;
    private readonly IDataManager dataManager;
    private readonly DebugRenderer debugRenderer;
    private readonly List<SheetName> SheetNames;
    private readonly ImmutableSortedDictionary<string, Type> SheetTypes;

    private string SelectedSheetName = string.Empty;
    private bool SortDirty = true;
    private short SortColumnIndex = 1;
    private ImGuiSortDirection SortDirection = ImGuiSortDirection.Ascending;
    private string SheetNameSearchTerm = string.Empty;

    private Language[] SheetLanguages = [];

    private string RowSearchTerm = string.Empty;
    private Language UsedLanguage;
    private Language SelectedLanguage;
    private CancellationTokenSource? FilterCTS;
    private ExcelHeaderFile? ExcelHeaderFile;
    private Type excelSheetRowType;
    private object? excelSheet;
    private Type? excelSheetType;
    private int excelSheetRowCount;
    private SheetColumn[] excelSheetColumns;

    public ExcelTab(ILogger<ExcelTab> Logger, TextService TextService, IDataManager DataManager, DebugRenderer DebugRenderer)
    {
        logger = Logger;
        textService = TextService;
        dataManager = DataManager;
        debugRenderer = DebugRenderer;

        var files = dataManager.GameData.GetFile<ExcelListFile>("exd/root.exl") ??
            throw new FileNotFoundException("Unable to load exd/root.exl!");

        SheetNames = files.ExdMap.Select(kv => (Id: kv.Value, Name: kv.Key)).ToList();
        SheetTypes = typeof(Addon).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<SheetAttribute>() is SheetAttribute sheetAttribute && !string.IsNullOrWhiteSpace(sheetAttribute.Name))
            .ToImmutableSortedDictionary(t => t.GetCustomAttribute<SheetAttribute>()!.Name!, t => t);

        SelectedLanguage = TextService.ClientLanguage.ToLumina();
    }

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("ExcelTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        DrawSheetList();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawSheet();
    }

    private void DrawSheetList()
    {
        using var sidebarchild = ImRaii.Child("SheetListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(-1);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##SheetTextSearch", textService.Translate("SearchBar.Hint"), ref SheetNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(SheetNameSearchTerm);
        var hasSearchTermAutoSelected = false;

        // TODO: dropdown filter

        using var table = ImRaii.Table("SheetListTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable | ImGuiTableFlags.NoSavedSettings, new Vector2(-1));
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        foreach (var (sheetId, sheetName) in SheetNames)
        {
            if (hasSearchTerm && !sheetName.Contains(SheetNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (hasSearchTermChanged && !hasSearchTermAutoSelected)
            {
                SelectSheet(sheetName);
                hasSearchTermAutoSelected = true;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(sheetId.ToString());

            ImGui.TableNextColumn(); // Name

            if (ImGui.Selectable(sheetName + $"##Sheet_{sheetName}", SelectedSheetName == sheetName, ImGuiSelectableFlags.SpanAllColumns))
            {
                SelectSheet(sheetName);
            }

            using var contextMenu = ImRaii.ContextPopupItem($"##Sheet_{sheetName}_Context");
            if (contextMenu)
            {
                if (!string.IsNullOrEmpty(sheetName) && ImGui.MenuItem("Copy name"))
                {
                    ImGui.SetClipboardText(sheetName);
                }

                // TODO: popout to window, pin to sidebar
            }
        }

        var sortSpecs = ImGui.TableGetSortSpecs();
        SortDirty |= sortSpecs.SpecsDirty;

        if (!SortDirty)
            return;

        SortColumnIndex = sortSpecs.Specs.ColumnIndex;
        SortDirection = sortSpecs.Specs.SortDirection;

        SheetNames.Sort((a, b) => SortColumnIndex switch
        {
            0 when SortDirection == ImGuiSortDirection.Ascending => CompareIdAsc(a, b),
            0 when SortDirection == ImGuiSortDirection.Descending => CompareIdDesc(a, b),
            1 when SortDirection == ImGuiSortDirection.Ascending => string.Compare(a.Name, b.Name, StringComparison.Ordinal),
            1 when SortDirection == ImGuiSortDirection.Descending => string.Compare(b.Name, a.Name, StringComparison.Ordinal),
            _ => 0,
        });

        sortSpecs.SpecsDirty = SortDirty = false;

        // these compare functions make sure sheets with id -1 are always at the bottom and sorted by name

        static int CompareIdAsc(SheetName a, SheetName b)
        {
            if (a.Id == b.Id)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            if (a.Id == -1)
                return 1;

            if (b.Id == -1)
                return -1;

            if (a.Id < b.Id)
                return -1;

            return 1;
        }

        static int CompareIdDesc(SheetName a, SheetName b)
        {
            if (a.Id == b.Id)
                return string.Compare(b.Name, a.Name, StringComparison.Ordinal);

            if (a.Id == -1)
                return 1;

            if (b.Id == -1)
                return -1;

            if (a.Id < b.Id)
                return 1;

            return -1;
        }
    }

    private void SelectSheet(string sheetName)
    {
        RowSearchTerm = string.Empty;

        var exhFilePath = string.Concat("exd/", sheetName, ".exh");
        ExcelHeaderFile = dataManager.GetFile<ExcelHeaderFile>(exhFilePath);
        if (ExcelHeaderFile == null)
        {
            SelectedSheetName = string.Empty;
            logger.LogWarning("Could not find {exhFilePath}", exhFilePath);
            return;
        }

        SelectedSheetName = sheetName;
        SheetLanguages = ExcelHeaderFile.Languages;

        var lang = SheetLanguages.Contains(SelectedLanguage)
            ? SelectedLanguage
            : SheetLanguages.Contains(Language.English)
                ? Language.English
                : Language.None;

        LoadSheet(lang);
    }

    private void LoadSheet(Language language)
    {
        if (ExcelHeaderFile == null)
            return;

        // move to LoadSheet

        if (ExcelHeaderFile.Header.Variant == Lumina.Data.Structs.Excel.ExcelVariant.Subrows)
        {
            logger.LogWarning("Subrows not supported yet! {exhFilePath}", ExcelHeaderFile.FilePath);
            return;
            // dataManager.Excel.GetSubrowSheet
        }

        excelSheetRowType = SheetTypes.TryGetValue(SelectedSheetName, out var sheetType) ? sheetType : typeof(RawRow);

        excelSheet = dataManager.Excel
            .GetType()
            .GetMethod("GetSheet")?
            .MakeGenericMethod(excelSheetRowType)
            .Invoke(dataManager.Excel, [language, null]);

        excelSheetType = excelSheet?.GetType();
        excelSheetRowCount = (int?)excelSheetType?.GetProperty("Count")?.GetValue(excelSheet, null) ?? 0;

        var firstRow = excelSheetType?.GetMethod("GetRowAt")?.Invoke(excelSheet, [0]);
        excelSheetColumns = firstRow == null
            ? []
            : excelSheetRowType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(propInfo => (propInfo.Name, propInfo.PropertyType))
                .ToArray();
    }

    private void DrawSheet()
    {
        if (string.IsNullOrEmpty(SelectedSheetName))
            return;

        if (excelSheet == null || excelSheetType == null)
        {
            ImGui.TextUnformatted("ExcelSheet not loaded");
            return;
        }

        using var hostchild = ImRaii.Child("SheetChild", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);

        ImGui.TextUnformatted(SelectedSheetName);
        ImGui.SameLine();
        ImGui.TextUnformatted("\u2022");
        ImGui.SameLine();
        ImGui.TextUnformatted(string.Concat("Rows: ", excelSheetRowCount));
        ImGui.SameLine();
        ImGui.TextUnformatted("\u2022");
        ImGui.SameLine();
        ImGui.TextUnformatted(string.Concat("Column: ", excelSheetColumns.Length));

        var supportsLanguages = !SheetLanguages.Contains(Language.None);

        const int LanguageSelectorWidth = 90;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (supportsLanguages ? LanguageSelectorWidth * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X : 0));
        var listDirty = ImGui.InputTextWithHint("##RowTextSearch", textService.Translate("SearchBar.Hint"), ref RowSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

        if (supportsLanguages)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(LanguageSelectorWidth * ImGuiHelpers.GlobalScale);
            using var dropdown = ImRaii.Combo("##Language", SelectedLanguage.ToString() ?? "Language...");
            if (dropdown)
            {
                foreach (var value in SheetLanguages)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == SelectedLanguage))
                    {
                        SelectedLanguage = value;
                        //Rows = dataManager.Excel.GetSheet<Addon>(SelectedLanguage.ToLumina()).ToArray();
                        listDirty |= true;
                    }
                }
            }
        }
        /*
        if (listDirty)
        {
            FilterCTS?.Cancel();
            FilterCTS = new();
            Task.Run(() => FilterList(FilterCTS.Token));
        }
        */
        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        // i hate these tables!
        using var table = ImRaii.Table("SheetRowTable", excelSheetColumns.Length,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible | ImGuiTableFlags.Resizable, new Vector2(-1));

        if (!table) return;

        foreach (var column in excelSheetColumns)
        {
            if (column.Name == "RowId")
            {
                ImGui.TableSetupColumn(column.Name, ImGuiTableColumnFlags.WidthFixed, 40);
            }
            else
            {
                ImGui.TableSetupColumn(
                    string.Concat(column.Name, "\n", column.Type.ReadableTypeName()),
                    ImGuiTableColumnFlags.WidthStretch,
                    MathF.Max(ImGui.CalcTextSize(column.Name).X, 120));
            }
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        unsafe
        {
            var imGuiListClipperPtr = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            imGuiListClipperPtr.Begin(excelSheetRowCount, ImGui.GetTextLineHeightWithSpacing());
            while (imGuiListClipperPtr.Step())
            {
                for (var i = imGuiListClipperPtr.DisplayStart; i < imGuiListClipperPtr.DisplayEnd; i++)
                {
                    if (i >= excelSheetRowCount)
                    {
                        return;
                    }

                    if (i >= 0)
                    {
                        DrawRow(i);
                    }
                }
            }

            imGuiListClipperPtr.End();
            imGuiListClipperPtr.Destroy();
        }
    }

    private void DrawRow(int i)
    {
        var row = excelSheetType?.GetMethod("GetRowAt")?.Invoke(excelSheet, [i]);
        if (row == null)
            return;

        ImGui.TableNextRow();

        foreach (var column in excelSheetColumns)
        {
            ImGui.TableNextColumn();
            var value = excelSheetRowType.GetProperty(column.Name)?.GetValue(row);

            switch (value)
            {
                case ReadOnlySeString seString:
                    debugRenderer.DrawSeStringSelectable(seString.AsSpan(), new NodeOptions()
                    {
                        AddressPath = new AddressPath(i),
                        RenderSeString = false,
                        Title = $"{SelectedSheetName}#{i} ({SelectedLanguage})",
                        Language = SelectedLanguage switch
                        {
                            Language.Japanese => ClientLanguage.Japanese,
                            Language.English => ClientLanguage.English,
                            Language.German => ClientLanguage.German,
                            Language.French => ClientLanguage.French,
                            _ => throw new ArgumentOutOfRangeException(nameof(SelectedLanguage)),
                        }
                    });
                    break;

                default:
                    ImGui.TextUnformatted(value?.ToString() ?? "???");
                    break;
            }
        }
    }

    /*
    private void FilterList(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(RowSearchTerm))
        {
            FilteredRows = null;
            return;
        }

        var list = new List<Addon>();

        for (var i = 0; i < Rows.Length && !cancellationToken.IsCancellationRequested; i++)
        {
            var row = Rows[i];
            if (row.RowId.ToString().Contains(SearchTerm)
             || row.Text.ToString().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
             || row.Text.ExtractText().Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                list.Add(row);
            }
        }

        FilteredRows = list.ToArray();
    }

    private void DrawRow(Addon row)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Text
        debugRenderer.DrawSeStringSelectable(row.Text.AsSpan(), new NodeOptions()
        {
            AddressPath = new AddressPath((nint)row.RowId),
            RenderSeString = false,
            Title = $"Addon#{row.RowId} ({SelectedLanguage})",
            Language = SelectedLanguage
        });
    }
    */
}
