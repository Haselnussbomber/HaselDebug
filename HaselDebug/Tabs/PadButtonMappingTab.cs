using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class PadButtonMappingTab(TextureService TextureService) : DebugTab
{
    public override void Draw()
    {
        using var table = ImRaii.Table("PadButtonMappingTable", 3, ImGuiTableFlags.Borders);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Requested Icon", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Displayed Icon", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var iconMapping = *(Entry**)((nint)(&RaptureAtkModule.Instance()->AtkFontManager) + 0x40);
        for (var i = 0; i < 30; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{i}");

            ImGui.TableNextColumn();
            TextureService.DrawGfd((uint)iconMapping[i].IconFrom, ImGui.GetTextLineHeightWithSpacing());
            ImGui.SameLine();
            ImGui.TextUnformatted($"{iconMapping[i].IconFrom}");

            ImGui.TableNextColumn();
            TextureService.DrawGfd((uint)iconMapping[i].IconTo, ImGui.GetTextLineHeightWithSpacing());
            ImGui.SameLine();
            ImGui.TextUnformatted($"{iconMapping[i].IconTo}");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Entry
    {
        public int IconFrom;
        public int IconTo;
    }
}
