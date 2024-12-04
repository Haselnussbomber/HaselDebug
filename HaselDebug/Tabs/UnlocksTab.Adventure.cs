using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    public void DrawSightseeingLog()
    {
        using var tab = ImRaii.TabItem("Sightseeing Log");
        if (!tab) return;

        using var table = ImRaii.Table("AdventureTabTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var i = 0u;
        foreach (var row in ExcelService.GetSheet<Adventure>())
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Completed
            var isComplete = PlayerState.Instance()->IsAdventureComplete(i);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isComplete ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isComplete.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon((uint)row.IconList);
            var clicked = ImGui.Selectable(row.Name.ExtractText());
            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (clicked)
                MapService.OpenMap(row.Level.Value);

            i++;
        }
    }
}
