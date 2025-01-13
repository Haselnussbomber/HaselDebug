using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Game.Enums;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
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
            new NameColumn(debugRenderer, mapService, unlocksTabUtils) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        var residentSheet = _excelService.GetSheet<TripleTriadCardResident>();
        Rows = _excelService.GetSheet<TripleTriadCard>()
            .Where(row => row.RowId != 0 && residentSheet.HasRow(row.RowId))
            .Select(row =>
            {
                _excelService.TryGetRow<TripleTriadCardResident>(row.RowId, out var resident);
                var hasItem = _excelService.TryFindRow<Item>(
                    itemRow => itemRow.ItemAction.Value.Type == (uint)ItemActionType.TripleTriadCard &&
                        itemRow.ItemAction.Value!.Data[0] == row.RowId,
                    out var item);
                return new TripleTriadCardEntry(row, resident, hasItem ? item : null);
            })
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

    private class NameColumn(DebugRenderer debugRenderer, MapService mapService, UnlocksTabUtils unlocksTabUtils) : ColumnString<TripleTriadCardEntry>
    {
        public override string ToName(TripleTriadCardEntry entry)
            => entry.Row.Name.ExtractText().StripSoftHypen();

        public override unsafe void DrawColumn(TripleTriadCardEntry entry)
        {
            debugRenderer.DrawIcon(88000 + entry.Row.RowId);
            var hasLevel = entry.ResidentRow.Location.TryGetValue<Level>(out var level);
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !hasLevel))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !hasLevel))
            {
                if (ImGui.Selectable(entry.Row.Name.ExtractText()))
                {
                    if (hasLevel)
                    {
                        mapService.OpenMap(level);
                    }
                }
            }

            if (entry.Item.HasValue && ImGui.IsItemHovered())
                unlocksTabUtils.DrawItemTooltip(entry.Item.Value);
        }
    }
}
