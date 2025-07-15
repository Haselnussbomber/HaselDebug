using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AgentMapEventMarkersTab : DebugTab
{
    private readonly ITextureProvider _textureProvider;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var agent = AgentMap.Instance();

        using var table = ImRaii.Table("AgentMapEventMarkersTable", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("LevelId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ObjectiveId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("MapId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Radius", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("TerritoryTypeId", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("TooltipString");
        ImGui.TableSetupScrollFreeze(6, 1);
        ImGui.TableHeadersRow();

        foreach (ref var marker in agent->EventMarkers)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Icon
            if (_textureProvider.TryGetFromGameIcon(marker.IconId, out var sharedTex) && sharedTex.TryGetWrap(out var tex, out var _))
                ImGui.Image(tex.Handle, new(ImGui.GetTextLineHeight()));

            ImGui.SameLine();
            ImGui.TextUnformatted(marker.IconId.ToString());

            ImGui.TableNextColumn(); // LevelId
            ImGui.TextUnformatted(marker.LevelId.ToString());

            ImGui.TableNextColumn(); // ObjectiveId
            ImGui.TextUnformatted(marker.ObjectiveId.ToString());

            ImGui.TableNextColumn(); // MapId
            ImGui.TextUnformatted(marker.MapId.ToString());

            ImGui.TableNextColumn(); // Radius
            ImGui.TextUnformatted(marker.Radius.ToString());

            ImGui.TableNextColumn(); // TerritoryTypeId
            ImGui.TextUnformatted(marker.TerritoryTypeId.ToString());

            ImGui.TableNextColumn(); // TooltipString
            if (marker.TooltipString != null && marker.TooltipString->StringPtr.Value != null)
                ImGui.TextUnformatted(new ReadOnlySeStringSpan(marker.TooltipString->StringPtr.Value).ExtractText());
        }
    }
}
