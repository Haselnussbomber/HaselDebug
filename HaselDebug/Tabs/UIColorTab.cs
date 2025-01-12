using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UIColorTab(ExcelService ExcelService, TextService TextService) : DebugTab
{
    public override string Title => "UIColor";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("UIColorTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn(TextService.GetAddonText(4232)); // Dark
        ImGui.TableSetupColumn(TextService.GetAddonText(4233)); // Light
        ImGui.TableSetupColumn(TextService.GetAddonText(4234)); // Classic FF
        ImGui.TableSetupColumn(TextService.GetAddonText(4235)); // Clear Blue
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<UIColor>())
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn();
            var color = (Vector4)Color.FromABGR(row.UIForeground);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Dark", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.UIGlow);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_Light", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Unknown0);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_ClassicFF", ref color, ImGuiColorEditFlags.DisplayHex);

            ImGui.TableNextColumn();
            color = (Vector4)Color.FromABGR(row.Unknown1);
            ImGui.ColorEdit4($"##UIColor_{row.RowId}_ClearBlue", ref color, ImGuiColorEditFlags.DisplayHex);
        }
    }
}
