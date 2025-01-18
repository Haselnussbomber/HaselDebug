using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Strings;
using HaselCommon.Game.Enums;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards;

[RegisterSingleton]
public unsafe class TripleTriadCardsTable : Table<TripleTriadCardEntry>
{
    internal readonly ExcelService _excelService;

    public TripleTriadCardsTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        MapService mapService,
        UnlocksTabUtils unlocksTabUtils,
        SeStringEvaluatorService seStringEvaluator,
        LanguageProvider languageProvider) : base("OrchestrionRollsTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new NumberColumn(seStringEvaluator) {
                Label = "Number",
                Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort,
                Width = 75,
            },
            new NameColumn(debugRenderer, excelService, seStringEvaluator, mapService, unlocksTabUtils) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        var residentSheet = _excelService.GetSheet<TripleTriadCardResident>();
        var cardItems = _excelService.GetSheet<Item>()
            .Where(itemRow => itemRow.ItemAction.Value.Type == (uint)ItemActionType.TripleTriadCard)
            .ToDictionary(itemRow => (uint)itemRow.ItemAction.Value!.Data[0]);

        Rows = _excelService.GetSheet<TripleTriadCard>()
            .Where(row => row.RowId != 0 && residentSheet.HasRow(row.RowId) && cardItems.ContainsKey(row.RowId))
            .Select(row => new TripleTriadCardEntry(row, residentSheet.GetRow(row.RowId), cardItems[row.RowId]))
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<TripleTriadCardEntry>
    {
        public override string ToName(TripleTriadCardEntry entry)
            => entry.Row.RowId.ToString();

        public override int ToValue(TripleTriadCardEntry entry)
            => (int)entry.Row.RowId;
    }

    private class UnlockedColumn : ColumnBool<TripleTriadCardEntry>
    {
        public override unsafe bool ToBool(TripleTriadCardEntry entry)
            => UIState.Instance()->IsTripleTriadCardUnlocked((ushort)entry.Row.RowId);
    }

    private class NumberColumn(SeStringEvaluatorService seStringEvaluator) : ColumnString<TripleTriadCardEntry>
    {
        public override string ToName(TripleTriadCardEntry entry)
        {
            var isEx = entry.ResidentRow.UIPriority == 5;
            var order = (uint)entry.ResidentRow.Order;
            var addonRowId = isEx ? 9773u : 9772;
            return seStringEvaluator.EvaluateFromAddon(addonRowId, new() { LocalParameters = [order] }).ExtractText();
        }

        public override int Compare(TripleTriadCardEntry lhs, TripleTriadCardEntry rhs)
        {
            var result = lhs.ResidentRow.UIPriority.CompareTo(rhs.ResidentRow.UIPriority);
            if (result == 0)
                return lhs.ResidentRow.Order.CompareTo(rhs.ResidentRow.Order);
            return result;
        }
    }

    private class NameColumn(DebugRenderer debugRenderer, ExcelService excelService, SeStringEvaluatorService seStringEvaluator, MapService mapService, UnlocksTabUtils unlocksTabUtils) : ColumnString<TripleTriadCardEntry>
    {
        public override string ToName(TripleTriadCardEntry entry)
            => entry.Row.Name.ExtractText().StripSoftHypen();

        public string ToSearchName(TripleTriadCardEntry entry)
        {
            var str = ToName(entry);

            if (entry.Item.HasValue &&
                excelService.TryGetRow<TripleTriadCardResident>(entry.Item.Value.ItemAction.Value.Data[0], out var residentRow) &&
                excelService.TryGetRow<TripleTriadCardObtain>(residentRow.AcquisitionType, out var obtainRow) &&
                obtainRow.Unknown1 != 0)
            {
                str += "\n" + seStringEvaluator.EvaluateFromAddon(obtainRow.Unknown1, new SeStringContext()
                {
                    LocalParameters = [
                        residentRow.Acquisition.RowId,
                        residentRow.Location.RowId
                    ]
                }).ExtractText();
            }

            return str;
        }

        public override bool ShouldShow(TripleTriadCardEntry row)
        {
            var name = ToSearchName(row);
            if (FilterValue.Length == 0)
                return true;

            return FilterRegex?.IsMatch(name) ?? name.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);
        }

        public override unsafe void DrawColumn(TripleTriadCardEntry entry)
        {
            debugRenderer.DrawIcon(88000 + entry.Row.RowId);

            if (AgentLobby.Instance()->IsLoggedIn)
            {
                var hasLevel = entry.ResidentRow.Location.TryGetValue<Level>(out var level);
                var hasCfcEntry = entry.ResidentRow.Acquisition.Is<ContentFinderCondition>();

                using (Color.Transparent.Push(ImGuiCol.HeaderActive, !hasLevel && !hasCfcEntry))
                using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !hasLevel && !hasCfcEntry))
                {
                    if (ImGui.Selectable(ToName(entry)))
                    {
                        if (hasCfcEntry)
                        {
                            if (entry.ResidentRow.Acquisition.TryGetValue<ContentFinderCondition>(out var cfc))
                            {
                                if (cfc.ContentType.RowId == 30)
                                {
                                    UIModule.Instance()->ExecuteMainCommand(94); // can't open VVDFinder with the right instance :/
                                }
                                else
                                {
                                    AgentContentsFinder.Instance()->OpenRegularDuty(entry.ResidentRow.Acquisition.RowId);
                                }
                            }
                        }
                        else if (hasLevel)
                        {
                            mapService.OpenMap(level);
                        }
                    }
                }

                if (entry.Item.HasValue && ImGui.IsItemHovered())
                    unlocksTabUtils.DrawItemTooltip(entry.Item.Value);
            }
            else
            {
                ImGui.TextUnformatted(ToName(entry));
            }
        }
    }
}
