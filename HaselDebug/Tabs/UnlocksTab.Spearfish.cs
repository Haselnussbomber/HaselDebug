using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabSpearfish(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    TextService TextService) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Spearfish";
    public override bool DrawInChild => false;

    public override void Draw()
    {
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
