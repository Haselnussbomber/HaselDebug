using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabMounts(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    TextService TextService,
    UnlocksTabUtils UnlocksTabUtils) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Mounts";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("MountsTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var playerState = PlayerState.Instance();
        var actionManager = ActionManager.Instance();
        var player = Control.GetLocalPlayer();
        var currentId = 0u;
        if (player != null)
            currentId = player->Mount.MountId;

        foreach (var row in ExcelService.GetSheet<Mount>())
        {
            if (row.RowId == 0 || row.UIPriority == 0)
                continue;

            var isUnlocked = playerState->IsMountUnlocked(row.RowId);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(row.Icon);
            var name = TextService.GetMountName(row.RowId);
            var canUse = isUnlocked && actionManager->GetActionStatus(ActionType.Mount, row.RowId) == 0;
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canUse))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canUse))
            {
                if (canUse)
                {
                    if (ImGui.Selectable(name, currentId == row.RowId))
                        actionManager->UseAction(ActionType.Mount, row.RowId);
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

                UnlocksTabUtils.DrawTooltip(
                    row.Icon,
                    name,
                    null,
                    ExcelService.TryGetRow<MountTransient>(row.RowId, out var transient) && !transient.DescriptionEnhanced.IsEmpty
                        ? transient.DescriptionEnhanced.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
