using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Mounts.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<Mount>
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

    public override string ToName(Mount row)
        => _textService.GetMountName(row.RowId);

    public override unsafe void DrawColumn(Mount row)
    {
        _debugRenderer.DrawIcon(row.Icon);

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

            _unlocksTabUtils.DrawTooltip(
                row.Icon,
                name,
                default,
                _excelService.TryGetRow<MountTransient>(row.RowId, out var transient)
                    ? transient.DescriptionEnhanced
                    : default);
        }
    }
}
