using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabOrchestrion(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    UnlocksTabUtils UnlocksTabUtils) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Orchestrion Rolls";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("OrchestrionTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 250);
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
            DebugRenderer.DrawIcon(uiParam.OrchestrionCategory.Value.Icon);
            ImGui.TextUnformatted(categoryName);

            ImGui.TableNextColumn(); // Name
            var name = row.Name.ExtractText().StripSoftHypen();
            using (Color.Transparent.Push(ImGuiCol.HeaderActive))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
                ImGui.Selectable(name);

            if (ImGui.IsItemHovered())
            {
                UnlocksTabUtils.DrawTooltip(
                    uiParam.OrchestrionCategory.Value.Icon,
                    name,
                    categoryName,
                    !row.Description.IsEmpty
                        ? row.Description.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
