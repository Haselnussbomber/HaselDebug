using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    public void DrawOrchestrion()
    {
        using var tab = ImRaii.TabItem("Orchestrion Rolls");
        if (!tab) return;

        using var table = ImRaii.Table("OrchestrionTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var playerState = PlayerState.Instance();

        foreach (var row in ExcelService.GetSheet<Orchestrion>())
        {
            if (row.RowId == 0 || !ExcelService.TryGetRow<OrchestrionUiparam>(row.RowId, out var uiParam))
                continue;

            if (uiParam.OrchestrionCategory.RowId == 0 || !uiParam.OrchestrionCategory.IsValid)
                continue;

            var isUnlocked = playerState->IsOrchestrionRollUnlocked(row.RowId);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Category
            var categoryName = uiParam.OrchestrionCategory.Value.Name.ExtractText().StripSoftHypen();
            ImGui.TextUnformatted(categoryName);

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(uiParam.OrchestrionCategory.Value.Icon);
            var name = row.Name.ExtractText().StripSoftHypen();
            using (Color.Transparent.Push(ImGuiCol.HeaderActive))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
                ImGui.Selectable(name);

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (!tooltip) continue;

                using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
                if (!popuptable) continue;

                ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 + ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, 300);

                ImGui.TableNextColumn(); // Icon
                TextureService.DrawIcon(uiParam.OrchestrionCategory.Value.Icon, 40);

                ImGui.TableNextColumn(); // Text
                using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, ImGui.GetStyle().ItemInnerSpacing.X);
                using var indent = ImRaii.PushIndent(1);

                ImGui.TextUnformatted(name);
                ImGuiUtils.PushCursorY(-3);
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                    ImGui.TextUnformatted(categoryName);
                ImGuiUtils.PushCursorY(1);

                // separator
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
                ImGuiUtils.PushCursorY(4);

                ImGuiHelpers.SafeTextWrapped(row.Description.ExtractText().StripSoftHypen());
            }
        }
    }
}