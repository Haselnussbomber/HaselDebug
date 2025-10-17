using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AgentMapEventMarkersTab : DebugTab
{
    private readonly ITextureProvider _textureProvider;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var agent = AgentMap.Instance();

        using var table = ImRaii.Table("AgentMapEventMarkersTable"u8, 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("LevelId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ObjectiveId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("MapId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Radius"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("TerritoryTypeId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
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
            ImGui.Text(marker.IconId.ToString());

            ImGui.TableNextColumn(); // LevelId
            ImGui.Text(marker.LevelId.ToString());

            ImGui.TableNextColumn(); // ObjectiveId
            ImGui.Text(marker.ObjectiveId.ToString());

            ImGui.TableNextColumn(); // MapId
            ImGui.Text(marker.MapId.ToString());

            ImGui.TableNextColumn(); // Radius
            ImGui.Text(marker.Radius.ToString());

            ImGui.TableNextColumn(); // TerritoryTypeId
            ImGui.Text(marker.TerritoryTypeId.ToString());

            ImGui.TableNextColumn(); // TooltipString
            if (marker.TooltipString != null && marker.TooltipString->StringPtr.Value != null)
                ImGui.Text(new ReadOnlySeStringSpan(marker.TooltipString->StringPtr.Value).ToString());
        }
    }
}
