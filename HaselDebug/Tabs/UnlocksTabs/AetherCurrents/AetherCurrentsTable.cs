using System.Collections.Generic;
using System.Globalization;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents;

[RegisterSingleton]
public unsafe class AetherCurrentsTable : Table<AetherCurrentEntry>
{
    internal readonly ExcelService _excelService;
    internal readonly Dictionary<uint, uint> AetherCurrentEObjCache = [];
    internal readonly Dictionary<uint, uint> EObjLevelCache = [];

    public AetherCurrentsTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        MapService mapService,
        TextService textService,
        LanguageProvider languageProvider) : base("AetherCurrentsTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new CompletedColumn() {
                Label = "Completed",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new ZoneColumn() {
                Label = "Zone",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 235,
            },
            new LocationColumn(this, debugRenderer, mapService, textService, excelService) {
                Label = "Location",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows.Clear();

        foreach (var row in _excelService.GetSheet<AetherCurrentCompFlgSet>())
        {
            var currentNumber = 1;
            var lastWasQuest = false;
            foreach (var aetherCurrent in row.AetherCurrents)
            {
                if (!aetherCurrent.IsValid) continue;

                var isQuest = aetherCurrent.Value.Quest.IsValid;
                if (isQuest)
                {
                    lastWasQuest = true;
                }
                else if (lastWasQuest)
                {
                    currentNumber = 1;
                    lastWasQuest = false;
                }

                Rows.Add(new AetherCurrentEntry(row, aetherCurrent.Value, currentNumber));
            }
        }
    }

    private class RowIdColumn : ColumnNumber<AetherCurrentEntry>
    {
        public override string ToName(AetherCurrentEntry entry)
            => entry.Row.RowId.ToString();

        public override int ToValue(AetherCurrentEntry row)
            => (int)row.Row.RowId;
    }

    private class CompletedColumn : ColumnBool<AetherCurrentEntry>
    {
        public override unsafe bool ToBool(AetherCurrentEntry entry)
            => PlayerState.Instance()->IsAetherCurrentUnlocked(entry.Row.RowId);
    }

    private class ZoneColumn : ColumnString<AetherCurrentEntry>
    {
        public override string ToName(AetherCurrentEntry entry)
            => entry.CompFlgSet.Territory.Value.Map.Value.PlaceName.Value.Name.ExtractText().StripSoftHypen();
    }

    private class LocationColumn(
        AetherCurrentsTable table,
        DebugRenderer debugRenderer,
        MapService mapService,
        TextService textService,
        ExcelService excelService) : ColumnString<AetherCurrentEntry>
    {
        public override string ToName(AetherCurrentEntry entry)
        {
            if (entry.Row.Quest.IsValid)
                return textService.GetQuestName(entry.Row.Quest.RowId) + " " + textService.GetENpcResidentName(entry.Row.Quest.Value.IssuerStart.RowId);
            else if (TryGetEObj(entry.Row, out var eobj))
                return textService.GetEObjName(eobj.RowId);

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

                mapService.OpenMap(quest.IssuerLocation.Value);
            }
            else
            {
                if (!TryGetEObj(entry.Row, out var eobj))
                    return;

                if (!TryGetLevel(eobj, out var level))
                    return;

                mapService.OpenMap(level);
            }
        }

        private void DrawQuest(int index, AetherCurrent aetherCurrent)
        {
            if (!TryGetFixedQuest(aetherCurrent, out var quest))
                return;

            debugRenderer.DrawIcon(quest.EventIconType.Value!.MapIconAvailable + 1, canCopy: false);
            ImGuiUtils.TextUnformattedColored(Color.Yellow, $"[#{index}] {textService.GetQuestName(quest.RowId)}");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{GetHumanReadableCoords(quest.IssuerLocation.Value)} | {textService.GetENpcResidentName(quest.IssuerStart.RowId)}");
        }

        private void DrawEObject(int index, AetherCurrent aetherCurrent)
        {
            if (!TryGetEObj(aetherCurrent, out var eobj))
                return;

            if (!TryGetLevel(eobj, out var level))
                return;

            debugRenderer.DrawIcon(60033, canCopy: false);
            ImGuiUtils.TextUnformattedColored(Color.Green, $"[#{index}] {textService.GetEObjName(eobj.RowId)}");
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

            if (!excelService.TryGetRow<Quest>(questId, out quest) || quest.IssuerLocation.RowId == 0)
                return false;

            return quest.IssuerLocation.IsValid;
        }

        private bool TryGetEObj(AetherCurrent aetherCurrent, out EObj eobj)
        {
            if (table.AetherCurrentEObjCache.TryGetValue(aetherCurrent.RowId, out var eobjRowId))
            {
                if (!excelService.TryGetRow<EObj>(eobjRowId, out eobj))
                    return false;
            }
            else
            {
                if (!excelService.TryFindRow<EObj>(row => row.Data == aetherCurrent.RowId, out eobj))
                    return false;

                table.AetherCurrentEObjCache.Add(aetherCurrent.RowId, eobj.RowId);
            }

            return true;
        }

        private bool TryGetLevel(EObj eobj, out Level level)
        {
            if (table.EObjLevelCache.TryGetValue(eobj.RowId, out var levelRowId))
            {
                if (!excelService.TryGetRow<Level>(levelRowId, out level))
                    return false;
            }
            else
            {
                if (!excelService.TryFindRow<Level>(row => row.Object.RowId == eobj.RowId, out level))
                    return false;

                table.EObjLevelCache.Add(eobj.RowId, level.RowId);
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
}
