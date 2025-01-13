using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.FashionAccessories;

[RegisterSingleton]
public unsafe class FashionAccessoriesTable : Table<Ornament>
{
    internal readonly ExcelService _excelService;

    public FashionAccessoriesTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base("FashionAccessoriesTable", languageProvider)
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
            new NameColumn(debugRenderer, textService, excelService, unlocksTabUtils) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Ornament>()
            .Where(row => row.RowId is not (0 or 22 or 25 or 26 or 32) && row.Order != 0 && row.Model != 0 && row.Icon != 0) // see AgentOrnamentNoteBook_Show
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<Ornament>
    {
        public override string ToName(Ornament row)
            => row.RowId.ToString();

        public override int ToValue(Ornament row)
            => (int)row.RowId;
    }

    private class UnlockedColumn : ColumnBool<Ornament>
    {
        public override unsafe bool ToBool(Ornament row)
            => PlayerState.Instance()->IsOrnamentUnlocked(row.RowId);
    }

    private class NameColumn(
        DebugRenderer debugRenderer,
        TextService textService,
        ExcelService excelService,
        UnlocksTabUtils unlocksTabUtils) : ColumnString<Ornament>
    {
        public override string ToName(Ornament row)
            => textService.GetOrnamentName(row.RowId);

        public override unsafe void DrawColumn(Ornament row)
        {
            var player = Control.GetLocalPlayer();
            var currentId = 0u;
            if (player != null)
                currentId = player->OrnamentData.OrnamentId;

            debugRenderer.DrawIcon(row.Icon);
            var name = ToName(row);
            var isUnlocked = PlayerState.Instance()->IsOrnamentUnlocked(row.RowId);
            var canUse = isUnlocked && ActionManager.Instance()->GetActionStatus(ActionType.Ornament, row.RowId) == 0;
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canUse))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canUse))
            {
                if (canUse)
                {
                    if (ImGui.Selectable(name, currentId == row.RowId))
                    {
                        ActionManager.Instance()->UseAction(ActionType.Ornament, row.RowId);
                    }
                }
                else
                {
                    ImGui.TextUnformatted(name);
                }
            }

            if (ImGui.IsItemHovered())
            {
                if (canUse)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                unlocksTabUtils.DrawTooltip(
                    row.Icon,
                    name,
                    null,
                    excelService.TryGetRow<OrnamentTransient>(row.RowId, out var transient) && !transient.Unknown0.IsEmpty
                        ? transient.Unknown0.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
