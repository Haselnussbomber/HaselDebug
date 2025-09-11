using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MainCommandsTab : DebugTab
{
    private readonly ITextureProvider _textureProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var agentHud = AgentHUD.Instance();
        var mcAggreModule = UIModule.Instance()->GetMcAggreModule();

        using var table = ImRaii.Table("MainCommandsTable"u8, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Used"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Failed"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        foreach (var row in _excelService.GetSheet<MainCommand>())
        {
            if (row.RowId == 0 || row.Icon == 0) continue;

            var isEnabled = agentHud->IsMainCommandEnabled(row.RowId);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.Text(row.RowId.ToString());

            ImGui.TableNextColumn(); // Used
            if (row.Unknown0 > 0 && row.Unknown0 < mcAggreModule->Usages.Length)
                ImGui.Text($"{mcAggreModule->Usages[row.Unknown0]}");

            ImGui.TableNextColumn(); // Failed
            if (row.Unknown0 > 0 && row.Unknown0 < mcAggreModule->FailedUsages.Length)
                ImGui.Text($"{mcAggreModule->FailedUsages[row.Unknown0]}");

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawIcon((uint)row.Icon);
            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X * 2); // idk why this bugs

            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !isEnabled))
            using (ImRaii.PushColor(ImGuiCol.HeaderHovered, ImGui.GetColorU32(ImGuiCol.HeaderHovered) & 0xFFFFFF | 0x30000000, !isEnabled))
            using (ImRaii.PushColor(ImGuiCol.HeaderActive, ImGui.GetColorU32(ImGuiCol.HeaderHovered) & 0xFFFFFF | 0x30000000, !isEnabled))
            using (ImRaii.PushColor(ImGuiCol.HeaderActive, ImGui.GetColorU32(ImGuiCol.HeaderHovered) & 0xFFFFFF | 0x30000000, !isEnabled))
            {
                if (ImGui.Selectable(row.Name.ToString()) && isEnabled)
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
                using var popuptable = ImRaii.Table("PopupTable"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
                if (!popuptable) return;

                ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 40 + ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthFixed, 300);

                ImGui.TableNextColumn(); // Icon
                _textureProvider.DrawIcon(row.Icon, 40);

                ImGui.TableNextColumn(); // Text
                using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, ImGui.GetStyle().ItemInnerSpacing.X);
                using var indent = ImRaii.PushIndent(1);

                ImGui.Text(row.Name.ToString());

                var categoryName = _excelService.TryGetRow<MainCommandCategory>(row.MainCommandCategory.RowId, out var category) ? category.Name.ToString() : string.Empty;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    ImGuiUtils.PushCursorY(-3);
                    using (ImRaii.PushColor(ImGuiCol.Text, Color.Grey.ToUInt()))
                        ImGui.Text(categoryName);
                }
                ImGuiUtils.PushCursorY(1);

                // separator
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
                ImGuiUtils.PushCursorY(4);

                ImGui.TextWrapped(row.Description.ToString());
            }
        }
    }
}
