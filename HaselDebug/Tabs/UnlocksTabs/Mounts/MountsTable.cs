using System.Linq;
using Dalamud.Utility;
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

namespace HaselDebug.Tabs.UnlocksTabs.Mounts;

[RegisterSingleton]
public unsafe class MountsTable : Table<Mount>
{
    internal readonly ExcelService _excelService;
    private readonly TextService _textService;

    public MountsTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base("MountsTable", languageProvider)
    {
        _excelService = excelService;
        _textService = textService;

        Columns = [
            RowIdColumn<Mount>.Create(),
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
        Rows = _excelService.GetSheet<Mount>()
            .Where(row => row.RowId != 0 && row.Order != 0 && row.Icon != 0 && !_textService.GetMountName(row.RowId).IsNullOrWhitespace())
            .ToList();
    }

    private class UnlockedColumn : ColumnBool<Mount>
    {
        public override unsafe bool ToBool(Mount row)
            => PlayerState.Instance()->IsMountUnlocked(row.RowId);
    }

    private class NameColumn(
        DebugRenderer debugRenderer,
        TextService textService,
        ExcelService excelService,
        UnlocksTabUtils unlocksTabUtils) : ColumnString<Mount>
    {
        public override string ToName(Mount row)
            => textService.GetMountName(row.RowId);

        public override unsafe void DrawColumn(Mount row)
        {
            debugRenderer.DrawIcon(row.Icon);

            var name = ToName(row);
            var isLoggedIn = AgentLobby.Instance()->IsLoggedIn;
            var isUnlocked = isLoggedIn && PlayerState.Instance()->IsMountUnlocked(row.RowId);
            var canUse = isUnlocked && ActionManager.Instance()->GetActionStatus(ActionType.Mount, row.RowId) == 0;
            var player = Control.GetLocalPlayer();
            var currentId = 0u;
            if (isLoggedIn && player != null)
                currentId = player->Mount.MountId;

            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canUse))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canUse))
            {
                if (canUse)
                {
                    if (ImGui.Selectable(name, currentId == row.RowId))
                        ActionManager.Instance()->UseAction(ActionType.Mount, row.RowId);
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
                    excelService.TryGetRow<MountTransient>(row.RowId, out var transient) && !transient.DescriptionEnhanced.IsEmpty
                        ? transient.DescriptionEnhanced.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
