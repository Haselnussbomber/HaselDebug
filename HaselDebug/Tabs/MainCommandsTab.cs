using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public unsafe class MainCommandsTab(DebugRenderer DebugRenderer, ExcelService ExcelService, TextureService TextureService) : DebugTab
{
    public override bool DrawInChild => false;
    public override void Draw()
    {
        var agentHud = AgentHUD.Instance();

        using var table = ImRaii.Table("MainCommandsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<MainCommand>())
        {
            if (row.RowId == 0 || row.Icon == 0) continue;

            var isEnabled = agentHud->IsMainCommandEnabled(row.RowId);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon((uint)row.Icon);
            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X * 2); // idk why this bugs. don't have that problem in the EmotesTab

            // WHAT DID HE DOO????? WOAH
            // yep, took a little creativity break here^^

            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !isEnabled))
            using (ImRaii.PushColor(ImGuiCol.HeaderHovered, ImGui.GetColorU32(ImGuiCol.HeaderHovered) & 0xFFFFFF | 0x30000000, !isEnabled))
            using (ImRaii.PushColor(ImGuiCol.HeaderActive, ImGui.GetColorU32(ImGuiCol.HeaderHovered) & 0xFFFFFF | 0x30000000, !isEnabled))
            using (ImRaii.PushColor(ImGuiCol.HeaderActive, ImGui.GetColorU32(ImGuiCol.HeaderHovered) & 0xFFFFFF | 0x30000000, !isEnabled))
            {
                if (ImGui.Selectable(row.Name.ExtractText()) && isEnabled)
                {
                    UIModule.Instance()->ExecuteMainCommand(row.RowId);
                }
            }

            if (ImGui.IsItemHovered())
            {
                if (isEnabled)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                using var tooltip = ImRaii.Tooltip();
                if (!tooltip) return;

                using var disabled = ImRaii.Disabled(!isEnabled);
                using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings);
                if (!popuptable) return;

                ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 + ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, 300);

                ImGui.TableNextColumn(); // Icon
                TextureService.DrawIcon(row.Icon, 40);

                ImGui.TableNextColumn(); // Text
                using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, ImGui.GetStyle().ItemInnerSpacing.X);
                using var indent = ImRaii.PushIndent(1);

                ImGui.TextUnformatted(row.Name.ExtractText());

                var category = ExcelService.GetRow<MainCommandCategory>(row.MainCommandCategory.Row);
                var categoryName = category?.Name.ExtractText() ?? string.Empty;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    ImGuiUtils.PushCursorY(-3);
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Grey))
                        ImGui.TextUnformatted(categoryName);
                }
                ImGuiUtils.PushCursorY(1);

                // separator
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
                ImGuiUtils.PushCursorY(4);

                ImGuiHelpers.SafeTextWrapped(row.Description.ExtractText());
            }
        }
    }
}
