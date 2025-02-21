using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AgentMapEventMarkersTab : DebugTab
{
    private readonly ITextureProvider _textureProvider;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var agent = AgentMap.Instance();

        using var table = ImRaii.Table("AgentMapEventMarkersTable", 6, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("LevelId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ObjectiveId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("MapId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("TerritoryTypeId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("TooltipString");
        ImGui.TableSetupScrollFreeze(6, 1);
        ImGui.TableHeadersRow();

        foreach (var marker in agent->EventMarkers)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Icon
            if (_textureProvider.GetFromGameIcon(marker.IconId).TryGetWrap(out var tex, out var _))
                ImGui.Image(tex.ImGuiHandle, new(ImGui.GetTextLineHeight()));
            ImGui.SameLine();
            ImGui.TextUnformatted(marker.IconId.ToString());

            ImGui.TableNextColumn(); // LevelId
            ImGui.TextUnformatted(marker.LevelId.ToString());

            ImGui.TableNextColumn(); // ObjectiveId
            ImGui.TextUnformatted(marker.ObjectiveId.ToString());

            ImGui.TableNextColumn(); // MapId
            ImGui.TextUnformatted(marker.MapId.ToString());

            ImGui.TableNextColumn(); // TerritoryTypeId
            ImGui.TextUnformatted(marker.TerritoryTypeId.ToString());

            ImGui.TableNextColumn(); // TooltipString
            ImGui.TextUnformatted(marker.TooltipString->ToString());
        }
    }
}
