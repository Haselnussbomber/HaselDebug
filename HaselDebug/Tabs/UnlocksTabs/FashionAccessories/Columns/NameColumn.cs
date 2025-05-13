using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.FashionAccessories.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<Ornament>
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

    public override string ToName(Ornament row)
        => _textService.GetOrnamentName(row.RowId);

    public override unsafe void DrawColumn(Ornament row)
    {
        var isLoggedIn = AgentLobby.Instance()->IsLoggedIn;
        var player = Control.GetLocalPlayer();
        var currentId = 0u;
        if (isLoggedIn && player != null)
            currentId = player->OrnamentData.OrnamentId;

        _debugRenderer.DrawIcon(row.Icon);
        var name = ToName(row);
        var isUnlocked = isLoggedIn && PlayerState.Instance()->IsOrnamentUnlocked(row.RowId);
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

            _unlocksTabUtils.DrawTooltip(
                row.Icon,
                name,
                default,
                _excelService.TryGetRow<OrnamentTransient>(row.RowId, out var transient)
                    ? transient.Text
                    : default);
        }
    }
}
