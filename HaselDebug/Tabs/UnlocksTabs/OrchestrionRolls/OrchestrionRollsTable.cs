using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.OrchestrionRolls;

[RegisterSingleton]
public unsafe class OrchestrionRollsTable : Table<OrchestrionRollEntry>
{
    internal readonly ExcelService _excelService;

    public OrchestrionRollsTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base(languageProvider)
    {
        _excelService = excelService;

        Columns = [
            EntryRowIdColumn<OrchestrionRollEntry, Orchestrion>.Create(),
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new CategoryColumn(debugRenderer) {
                Label = "Category",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 265,
            },
            new NumberColumn() {
                Label = "Number",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new NameColumn(unlocksTabUtils) {
                Label = "Name",
            }
        ];

        // TODO: Flags |= ImGuiTableFlags.SortMulti;
    }

    public override void LoadRows()
    {
        var uiParamSheet = _excelService.GetSheet<OrchestrionUiparam>();
        Rows = _excelService.GetSheet<Orchestrion>()
            .Where(row => row.RowId != 0 && uiParamSheet.HasRow(row.RowId))
            .Select(row =>
            {
                _excelService.TryGetRow<OrchestrionUiparam>(row.RowId, out var uiParam);
                return new OrchestrionRollEntry(row, uiParam);
            })
            .Where(entry => entry.UIParamRow.OrchestrionCategory.RowId != 0 && entry.UIParamRow.OrchestrionCategory.IsValid)
            .ToList();
    }

    private class UnlockedColumn : ColumnBool<OrchestrionRollEntry>
    {
        public override unsafe bool ToBool(OrchestrionRollEntry entry)
            => PlayerState.Instance()->IsOrchestrionRollUnlocked(entry.Row.RowId);
    }

    private class CategoryColumn(DebugRenderer debugRenderer) : ColumnString<OrchestrionRollEntry>
    {
        public override string ToName(OrchestrionRollEntry entry)
            => entry.UIParamRow.OrchestrionCategory.Value.Name.ExtractText().StripSoftHypen();

        public override void DrawColumn(OrchestrionRollEntry entry)
        {
            debugRenderer.DrawIcon(entry.UIParamRow.OrchestrionCategory.Value.Icon);
            ImGui.TextUnformatted(ToName(entry));
        }

        public override int Compare(OrchestrionRollEntry a, OrchestrionRollEntry b)
        {
            var result = base.Compare(a, b);
            if (result == 0)
                result = a.UIParamRow.Order.CompareTo(b.UIParamRow.Order);
            return result;
        }
    }

    private class NumberColumn : ColumnString<OrchestrionRollEntry>
    {
        public override string ToName(OrchestrionRollEntry entry)
            => entry.UIParamRow.Order == 65535 ? "\u2014" : $"{entry.UIParamRow.Order:000}";
    }

    private class NameColumn(UnlocksTabUtils unlocksTabUtils) : ColumnString<OrchestrionRollEntry>
    {
        public override string ToName(OrchestrionRollEntry entry)
            => entry.Row.Name.ExtractText().StripSoftHypen();

        public override unsafe void DrawColumn(OrchestrionRollEntry entry)
        {
            var name = ToName(entry);
            using (Color.Transparent.Push(ImGuiCol.HeaderActive))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
                ImGui.Selectable(name);

            if (ImGui.IsItemHovered())
            {
                unlocksTabUtils.DrawTooltip(
                    entry.UIParamRow.OrchestrionCategory.Value.Icon,
                    name,
                    entry.UIParamRow.OrchestrionCategory.Value.Name.ExtractText().StripSoftHypen(),
                    !entry.Row.Description.IsEmpty
                        ? entry.Row.Description.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
