using System.Collections.Generic;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Tabs.UnlocksTabs.Cutscenes.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes;

[RegisterSingleton, AutoConstruct]
public unsafe partial class CutscenesTable : Table<CutsceneEntry>
{
    private readonly ExcelService _excelService;
    private readonly WorkIndexColumn _workIndexColumn;
    private readonly SeenColumn _seenColumn;
    private readonly PathColumn _pathColumn;
    private readonly UsesColumn _usesColumn;

    private readonly Dictionary<uint, CutsceneEntry> _cutscenes = [];

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            EntryRowIdColumn<CutsceneEntry, Cutscene>.Create(),
            _workIndexColumn,
            _seenColumn,
            _pathColumn,
            _usesColumn,
        ];

        LineHeight = 0;
    }

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
}
