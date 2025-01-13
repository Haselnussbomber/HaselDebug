using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes;

[RegisterSingleton]
public unsafe class CutscenesTable : Table<CutsceneEntry>
{
    internal readonly ExcelService _excelService;
    private readonly Dictionary<uint, CutsceneEntry> _cutscenes = [];

    public CutscenesTable(ExcelService excelService, LanguageProvider languageProvider) : base("CutscenesTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            new WorkIndexColumn() {
                Label = "WorkIdx",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new SeenColumn() {
                Label = "Seen",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new PathColumn() {
                Label = "Path",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 315,
            },
            new UsesColumn {
                Label = "Uses",
            }
        ];

        LineHeight = 0;
    }

    public bool HideSpoilers = true;

    public override void LoadRows()
    {
        var cutsceneSheet = _excelService.GetSheet<Cutscene>();
        var cutsceneWorkIndexSheet = _excelService.GetSheet<CutsceneWorkIndex>();

        _cutscenes.Clear();

        for (var i = 0; i < cutsceneSheet.Count; i++)
        {
            var row = cutsceneSheet.GetRowAt(i);
            if (row.Path.IsEmpty)
                continue;

            var workIndexRow = cutsceneWorkIndexSheet.GetRowAt(i);
            if (workIndexRow.WorkIndex == 0)
                continue;

            _cutscenes.Add(row.RowId, new CutsceneEntry(i, row, workIndexRow, []));
        }

        foreach (var row in _excelService.GetSheet<CompleteJournal>())
        {
            foreach (var cutscene in row.Cutscene)
            {
                if (cutscene.RowId == 0) continue;
                if (_cutscenes.TryGetValue(cutscene.RowId, out var cEntry))
                    cEntry.Uses.Add((typeof(CompleteJournal), row.RowId, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<Lumina.Excel.Sheets.InstanceContent>())
        {
            if (row.Cutscene.RowId == 0)
                continue;

            if (!_excelService.TryFindRow<ContentFinderCondition>(cfcrow => cfcrow!.ContentLinkType == 1 && cfcrow.Content.RowId == row.RowId, out var cfc))
                continue;

            if (_cutscenes.TryGetValue(row.Cutscene.RowId, out var cEntry))
                cEntry.Uses.Add((typeof(Lumina.Excel.Sheets.InstanceContent), row.RowId, cfc.Name.ExtractText()));
        }

        foreach (var row in _excelService.GetSheet<PartyContentCutscene>())
        {
            if (row.Cutscene.RowId == 0)
                continue;

            if (!_excelService.TryFindRow<ContentFinderCondition>(cfcrow => cfcrow!.ContentLinkType == 1 && cfcrow.Content.RowId == row.RowId, out var cfc))
                continue;

            if (_cutscenes.TryGetValue(row.Cutscene.RowId, out var cEntry))
                cEntry.Uses.Add((typeof(PartyContentCutscene), row.RowId, cfc.Name.ExtractText()));
        }

        foreach (var row in _excelService.GetSheet<PublicContentCutscene>())
        {
            if (row.Cutscene.RowId == 0)
                continue;

            if (!_excelService.TryFindRow<ContentFinderCondition>(cfcrow => cfcrow!.ContentLinkType == 1 && cfcrow.Content.RowId == row.RowId, out var cfc))
                continue;

            if (_cutscenes.TryGetValue(row.Cutscene.RowId, out var cEntry))
                cEntry.Uses.Add((typeof(PublicContentCutscene), row.RowId, cfc.Name.ExtractText()));
        }

        foreach (var row in _excelService.GetSheet<Warp>())
        {
            if (row.StartCutscene.RowId != 0)
            {
                if (_cutscenes.TryGetValue(row.StartCutscene.RowId, out var cEntry))
                    cEntry.Uses.Add((typeof(Warp), row.RowId, !row.Name.IsEmpty ? row.Name.ExtractText() : row.Question.ExtractText()));
            }

            if (row.EndCutscene.RowId != 0)
            {
                if (_cutscenes.TryGetValue(row.EndCutscene.RowId, out var cEntry))
                    cEntry.Uses.Add((typeof(Warp), row.RowId, !row.Name.IsEmpty ? row.Name.ExtractText() : row.Question.ExtractText()));
            }
        }

        Rows = [.. _cutscenes.Values];
    }

    private class WorkIndexColumn : ColumnNumber<CutsceneEntry>
    {
        public override string ToName(CutsceneEntry entry)
            => entry.WorkIndexRow.WorkIndex.ToString();

        public override int ToValue(CutsceneEntry entry)
            => entry.WorkIndexRow.WorkIndex;
    }

    private class SeenColumn : ColumnBool<CutsceneEntry>
    {
        public override unsafe bool ToBool(CutsceneEntry entry)
            => UIState.Instance()->IsCutsceneSeen((uint)entry.Index);
    }

    private class PathColumn : ColumnString<CutsceneEntry>
    {
        public override string ToName(CutsceneEntry entry)
            => entry.Row.Path.ExtractText();
    }

    private class UsesColumn : ColumnString<CutsceneEntry>
    {
        public override string ToName(CutsceneEntry entry)
            => string.Join('\n', entry.Uses.Select(e => $"{e.SheetType.Name}#{e.RowId}{(e.Label.IsNullOrEmpty() ? string.Empty : $" ({e.Label})")}"));
    }
}
