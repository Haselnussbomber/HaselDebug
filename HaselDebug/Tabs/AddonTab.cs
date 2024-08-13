using System.Linq;
using Dalamud.Interface.Utility.Raii;
using ExdSheets.Sheets;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AddonTab(DebugRenderer DebugRenderer, ExdSheets.Module ExdModule) : DebugTab
{
    public override bool DrawInChild => false;
    public override void Draw()
    {
        using var table = ImRaii.Table("AddonTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var sheet = ExdModule.GetSheet<Addon>();

        var imGuiListClipperPtr = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        imGuiListClipperPtr.Begin(sheet.Count, ImGui.GetTextLineHeightWithSpacing());

        while (imGuiListClipperPtr.Step())
        {
            foreach (var row in sheet.Skip(imGuiListClipperPtr.DisplayStart).Take(imGuiListClipperPtr.DisplayEnd - imGuiListClipperPtr.DisplayStart))
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn(); // RowId
                ImGui.TextUnformatted(row.RowId.ToString());

                ImGui.TableNextColumn(); // Text
                DebugRenderer.DrawSeStringSelectable(row.Text.AsSpan(), new NodeOptions()
                {
                    RenderSeString = false,
                    Title = $"Addon#{row.RowId}"
                });
            }
        }

        imGuiListClipperPtr.End();
        imGuiListClipperPtr.Destroy();
    }
}
