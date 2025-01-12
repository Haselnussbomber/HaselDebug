using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabEmotes(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Emotes";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        var agentEmote = AgentEmote.Instance();

        using var table = ImRaii.Table("EmotesTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Can Use", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupScrollFreeze(6, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<Emote>())
        {
            if (row.RowId == 0) continue;

            ImGui.TableNextRow();
            var canUse = agentEmote->CanUseEmote((ushort)row.RowId);

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Name
            var name = row.Name.ExtractText();
            var hasName = !string.IsNullOrWhiteSpace(name);
            DebugRenderer.DrawIcon(row.Icon, sameLine: hasName);
            if (hasName)
            {
                using (ImRaii.Disabled(!canUse))
                {
                    var clicked = ImGui.Selectable(name);
                    if (ImGui.IsItemHovered())
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (clicked)
                    {
                        AgentEmote.Instance()->ExecuteEmote((ushort)row.RowId, addToHistory: false);
                    }
                }
            }

            ImGui.TableNextColumn(); // Can Use
            ImGui.TextUnformatted(canUse.ToString());
        }
    }
}
