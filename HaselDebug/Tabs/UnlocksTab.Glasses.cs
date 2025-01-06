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

public unsafe class UnlocksTabGlasses(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    TextService TextService,
    UnlocksTabUtils UnlocksTabUtils) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Glasses";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("GlassesTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var playerState = PlayerState.Instance();

        foreach (var row in ExcelService.GetSheet<Glasses>())
        {
            if (row.RowId == 0)
                continue;

            var isUnlocked = playerState->IsGlassesUnlocked((ushort)row.RowId);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon((uint)row.Icon);
            var name = TextService.GetGlassesName(row.RowId);
            using (Color.Transparent.Push(ImGuiCol.HeaderActive))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
                ImGui.Selectable(name);

            if (ImGui.IsItemHovered())
            {
                UnlocksTabUtils.DrawTooltip(
                    (uint)row.Icon,
                    name,
                    null,
                    !row.Description.IsEmpty
                        ? row.Description.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
