using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class UnlocksTabTripleTriadCards(DebugRenderer DebugRenderer, ExcelService ExcelService) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Triple Triad Cards";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        var uiState = UIState.Instance();

        using var table = ImRaii.Table("TripleTriadCardsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Collected", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<TripleTriadCard>())
        {
            if (row.RowId == 0)
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Collected
            var isCollected = uiState->IsTripleTriadCardUnlocked((ushort)row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isCollected ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isCollected.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(88000 + row.RowId);
            ImGui.TextUnformatted(row.Name.ExtractText());
        }
    }
}
