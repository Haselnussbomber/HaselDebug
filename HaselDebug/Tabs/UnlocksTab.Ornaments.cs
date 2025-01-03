using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class UnlocksTabOrnaments(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    TextService TextService,
    TextureService TextureService) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Fashion Accessories";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("OrnamentsTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
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
            currentId = player->OrnamentData.OrnamentId;

        foreach (var row in ExcelService.GetSheet<Ornament>())
        {
            // see AgentOrnamentNoteBook_Show
            if (row.RowId is 0 or 22 or 25 or 26 or 32 || row.Order == 0 || row.Model == 0 || row.Icon == 0)
                continue;

            var isUnlocked = playerState->IsOrnamentUnlocked((ushort)row.RowId);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(row.Icon);
            var name = TextService.GetOrnamentName(row.RowId);
            var canUse = isUnlocked && actionManager->GetActionStatus(ActionType.Ornament, row.RowId) == 0;
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !canUse))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !canUse))
            {
                if (canUse)
                {
                    if (ImGui.Selectable(name, currentId == row.RowId))
                    {
                        actionManager->UseAction(ActionType.Ornament, row.RowId);
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

                using var tooltip = ImRaii.Tooltip();
                if (!tooltip) continue;

                using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
                if (!popuptable) continue;

                ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 + ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, 300);

                ImGui.TableNextColumn(); // Icon
                TextureService.DrawIcon((uint)row.Icon, 40);

                ImGui.TableNextColumn(); // Text
                using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, ImGui.GetStyle().ItemInnerSpacing.X);
                using var indent = ImRaii.PushIndent(1);

                ImGui.TextUnformatted(name);

                if (ExcelService.TryGetRow<OrnamentTransient>(row.RowId, out var transient))
                {
                    // separator
                    var pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
                    ImGuiUtils.PushCursorY(4);

                    ImGuiHelpers.SafeTextWrapped(transient.Unknown0.ExtractText().StripSoftHypen());
                }
            }
        }
    }
}
