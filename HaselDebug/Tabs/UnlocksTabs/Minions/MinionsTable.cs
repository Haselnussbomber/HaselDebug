using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Minions;

[RegisterSingleton]
public unsafe class MinionsTable : Table<Companion>
{
    internal readonly ExcelService _excelService;

    public MinionsTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base(languageProvider)
    {
        _excelService = excelService;

        Columns = [
            RowIdColumn<Companion>.Create(),
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
        Rows = _excelService.GetSheet<Companion>()
            .Where(row => row.RowId != 0 && row.Order != 0)
            .ToList();
    }

    private class UnlockedColumn : ColumnBool<Companion>
    {
        public override unsafe bool ToBool(Companion row)
            => UIState.Instance()->IsCompanionUnlocked(row.RowId);
    }

    private class NameColumn(
        DebugRenderer debugRenderer,
        TextService textService,
        ExcelService excelService,
        UnlocksTabUtils unlocksTabUtils) : ColumnString<Companion>
    {
        public override string ToName(Companion row)
            => textService.GetCompanionName(row.RowId);

        public override unsafe void DrawColumn(Companion row)
        {
            debugRenderer.DrawIcon(row.Icon);

            var name = ToName(row);
            var isLoggedIn = AgentLobby.Instance()->IsLoggedIn;
            var isUnlocked = isLoggedIn && UIState.Instance()->IsCompanionUnlocked(row.RowId);
            var canUse = isUnlocked && ActionManager.Instance()->GetActionStatus(ActionType.Companion, row.RowId) == 0;
            var player = Control.GetLocalPlayer();
            var currentId = 0u;
            if (isLoggedIn && player != null && player->CompanionData.CompanionObject != null)
                currentId = player->CompanionData.CompanionObject->BaseId;

            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canUse))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canUse))
            {
                if (canUse)
                {
                    if (ImGui.Selectable(name, currentId == row.RowId))
                        ActionManager.Instance()->UseAction(ActionType.Companion, row.RowId);
                }
                else
                {
                    ImGui.Selectable(name);
                }
            }

            if (ImGui.IsItemHovered())
            {
                if (canUse)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                unlocksTabUtils.DrawTooltip(
                    row.Icon,
                    name,
                    null,
                    excelService.TryGetRow<CompanionTransient>(row.RowId, out var transient) && !transient.DescriptionEnhanced.IsEmpty
                        ? transient.DescriptionEnhanced.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
