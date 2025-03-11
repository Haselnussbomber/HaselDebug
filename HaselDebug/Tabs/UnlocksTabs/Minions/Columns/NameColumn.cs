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

namespace HaselDebug.Tabs.UnlocksTabs.Minions.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<Companion>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(Companion row)
        => _textService.GetCompanionName(row.RowId);

    public override unsafe void DrawColumn(Companion row)
    {
        _debugRenderer.DrawIcon(row.Icon);

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

            _unlocksTabUtils.DrawTooltip(
                row.Icon,
                name,
                null,
                _excelService.TryGetRow<CompanionTransient>(row.RowId, out var transient) && !transient.DescriptionEnhanced.IsEmpty
                    ? transient.DescriptionEnhanced.ExtractText().StripSoftHyphen()
                    : null);
        }
    }
}
