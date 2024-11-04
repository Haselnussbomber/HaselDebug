using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class SatisfactionSupplyTab(ExcelService ExcelService, TextService TextService) : DebugTab
{
    public override bool DrawInChild => false;
    public override void Draw()
    {
        var satisfactionSupply = SatisfactionSupplyManager.Instance();

        using var table = ImRaii.Table("SatisfactionSupply", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Satisfaction", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Used Allowance", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<SatisfactionNpc>())
        {
            if (row.RowId == 0)
                continue;

            var index = (int)row.RowId - 1;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Name
            ImGui.TextUnformatted(TextService.GetENpcResidentName(row.Npc.RowId));

            ImGui.TableNextColumn(); // Satisfaction
            ImGui.TextUnformatted($"{satisfactionSupply->Satisfaction[index]}/{row.SatisfactionNpcParams[satisfactionSupply->SatisfactionRanks[index]].SatisfactionRequired}");

            ImGui.TableNextColumn(); // Rank
            ImGui.TextUnformatted($"{satisfactionSupply->SatisfactionRanks[index]}");

            ImGui.TableNextColumn(); // UsedAllowance
            ImGui.TextUnformatted($"{satisfactionSupply->UsedAllowances[index]}");
        }
    }
}
