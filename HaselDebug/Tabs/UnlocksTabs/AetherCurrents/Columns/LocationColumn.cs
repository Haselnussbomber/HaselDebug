using System.Globalization;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents.Columns;

[RegisterTransient, AutoConstruct]
public partial class LocationColumn : ColumnString<AetherCurrentEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly MapService _mapService;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;

    [AutoConstructIgnore]
    private AetherCurrentsTable _table;

    public void SetTable(AetherCurrentsTable table)
    {
        _table = table;
    }

    public override string ToName(AetherCurrentEntry entry)
    {
        if (entry.Row.Quest.IsValid)
            return _textService.GetQuestName(entry.Row.Quest.RowId) + " " + _textService.GetENpcResidentName(entry.Row.Quest.Value.IssuerStart.RowId);
        else if (TryGetEObj(entry.Row, out var eobj))
            return _textService.GetEObjName(eobj.RowId);

        return string.Empty;
    }

    public override unsafe void DrawColumn(AetherCurrentEntry entry)
    {
        var clicked = ImGui.Selectable($"###AetherCurrentSelectable_{entry.Row.RowId}");

        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        ImGui.SameLine(0, 0);

        var isQuest = entry.Row.Quest.IsValid;
        if (isQuest)
            DrawQuest(entry.Number, entry.Row);
        else
            DrawEObject(entry.Number, entry.Row);

        if (!clicked)
            return;

        if (isQuest)
        {
            if (!TryGetFixedQuest(entry.Row, out var quest))
                return;

            _mapService.OpenMap(quest.IssuerLocation.Value);
        }
        else
        {
            if (!TryGetEObj(entry.Row, out var eobj))
                return;

            if (!TryGetLevel(eobj, out var level))
                return;

            _mapService.OpenMap(level);
        }
    }

    private void DrawQuest(int index, AetherCurrent aetherCurrent)
    {
        if (!TryGetFixedQuest(aetherCurrent, out var quest))
            return;

        _debugRenderer.DrawIcon(quest.EventIconType.Value!.MapIconAvailable + 1, canCopy: false);
        ImGuiUtils.TextUnformattedColored(Color.Yellow, $"[#{index}] {_textService.GetQuestName(quest.RowId)}");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{GetHumanReadableCoords(quest.IssuerLocation.Value)} | {_textService.GetENpcResidentName(quest.IssuerStart.RowId)}");
    }

    private void DrawEObject(int index, AetherCurrent aetherCurrent)
    {
        if (!TryGetEObj(aetherCurrent, out var eobj))
            return;

        if (!TryGetLevel(eobj, out var level))
            return;

        _debugRenderer.DrawIcon(60033, canCopy: false);
        ImGuiUtils.TextUnformattedColored(Color.Green, $"[#{index}] {_textService.GetEObjName(eobj.RowId)}");
        ImGui.SameLine();
        ImGui.TextUnformatted(GetHumanReadableCoords(level));
    }

    private bool TryGetFixedQuest(AetherCurrent aetherCurrent, out Quest quest)
    {
        var questId = aetherCurrent.Quest.RowId;

        // Some AetherCurrents link to the wrong Quest.
        // See https://github.com/Haselnussbomber/HaselTweaks/issues/15

        // The Dravanian Forelands (CompFlgSet#2)
        if (aetherCurrent.RowId == 2818065 && questId == 67328) // Natural Repellent
            questId = 67326; // Stolen Munitions
        else if (aetherCurrent.RowId == 2818066 && questId == 67334) // Chocobo's Last Stand
            questId = 67333; // The Hunter Becomes the Kweh

        // The Churning Mists (CompFlgSet#4)
        else if (aetherCurrent.RowId == 2818096 && questId == 67365) // The Unceasing Gardener
            questId = 67364; // Hide Your Moogles

        // The Sea of Clouds (CompFlgSet#5)
        else if (aetherCurrent.RowId == 2818110 && questId == 67437) // Search and Rescue
            questId = 67410; // Honoring the Past

        // Thavnair (CompFlgSet#21)
        else if (aetherCurrent.RowId == 2818328 && questId == 70030) // Curing What Ails
            questId = 69793; // In Agama's Footsteps

        if (!_excelService.TryGetRow<Quest>(questId, out quest) || quest.IssuerLocation.RowId == 0)
            return false;

        return quest.IssuerLocation.IsValid;
    }

    private bool TryGetEObj(AetherCurrent aetherCurrent, out EObj eobj)
    {
        if (_table.AetherCurrentEObjCache.TryGetValue(aetherCurrent.RowId, out var eobjRowId))
        {
            if (!_excelService.TryGetRow<EObj>(eobjRowId, out eobj))
                return false;
        }
        else
        {
            if (!_excelService.TryFindRow<EObj>(row => row.Data == aetherCurrent.RowId, out eobj))
                return false;

            _table.AetherCurrentEObjCache.Add(aetherCurrent.RowId, eobj.RowId);
        }

        return true;
    }

    private bool TryGetLevel(EObj eobj, out Level level)
    {
        if (_table.EObjLevelCache.TryGetValue(eobj.RowId, out var levelRowId))
        {
            if (!_excelService.TryGetRow<Level>(levelRowId, out level))
                return false;
        }
        else
        {
            if (!_excelService.TryFindRow<Level>(row => row.Object.RowId == eobj.RowId, out level))
                return false;

            _table.EObjLevelCache.Add(eobj.RowId, level.RowId);
        }

        return true;
    }

    private string GetHumanReadableCoords(Level level)
    {
        var coords = MapService.GetCoords(level);
        var x = coords.X.ToString("0.0", CultureInfo.InvariantCulture);
        var y = coords.Y.ToString("0.0", CultureInfo.InvariantCulture);
        return string.Format("X: {0}, Y: {1}", x, y);
    }
}
