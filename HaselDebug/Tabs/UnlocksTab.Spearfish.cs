using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    public void DrawSpearfish()
    {
        using var tab = ImRaii.TabItem("Spearfish");
        if (!tab) return;

        var playerState = PlayerState.Instance();
        if (playerState->IsLoaded != 1)
        {
            ImGui.TextUnformatted("PlayerState not loaded");
            return;
        }

        using var table = ImRaii.Table("SpearfishTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Caught", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<SpearfishingItem>())
        {
            if (row.RowId == 0)
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Caught
            var isCaught = playerState->IsSpearfishCaught(row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isCaught ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isCaught.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(row.Item.ValueNullable?.Icon ?? 0);
            if (ImGui.Selectable(TextService.GetItemName(row.Item.RowId)))
                AgentFishGuide.Instance()->OpenForItemId(row.Item.RowId, true);
        }
    }
}
