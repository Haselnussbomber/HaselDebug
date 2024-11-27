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
    public void DrawCurrents()
    {
        using var tab = ImRaii.TabItem("Aether Currents");
        if (!tab) return;

        using var table = ImRaii.Table("CurrentsTabTable", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var i = 0u;
        foreach (var row in ExcelService.GetSheet<AetherCurrentCompFlgSet>())
        {
            var currentnumber = 0;
            foreach (var current in row.AetherCurrents)
            {
                if (!current.IsValid) continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Index
                ImGui.TextUnformatted(i.ToString());

                ImGui.TableNextColumn(); // RowId
                ImGui.TextUnformatted(current.RowId.ToString());

                ImGui.TableNextColumn(); // Completed
                var isComplete = PlayerState.Instance()->IsAetherCurrentUnlocked(current.RowId);
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isComplete ? Color.Green : Color.Red)))
                    ImGui.TextUnformatted(isComplete.ToString());

                ImGui.TableNextColumn(); // Name
                ImGui.TextUnformatted(row.Territory.Value.Map.Value.PlaceName.Value.Name.ExtractText());

                ImGui.TableNextColumn(); // Quest

                var clicked = ImGui.Selectable(current.Value.Quest.IsValid ? "[QUEST] " + current.Value.Quest.Value.Name.ExtractText() : $"{row.Territory.Value.Map.Value.PlaceName.Value.Name.ExtractText()} Aether Current #{currentnumber+=1}");
                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (clicked)
                {
                    if (current.Value.Quest.IsValid)
                    {
                        MapService.OpenMap(current.Value.Quest.Value.IssuerLocation.Value);
                    }
                    else
                    {
                        if (!ExcelService.TryFindRow<EObj>(row => row.Data == current.RowId, out var eobj))
                            continue;
                        if (!ExcelService.TryFindRow<Level>(row => row.Object.RowId == eobj.RowId, out var level))
                            continue;
                        MapService.OpenMap(level);
                    }
                }
                i++;
            }
        }
    }
}
