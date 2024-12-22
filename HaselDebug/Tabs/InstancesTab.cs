using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Attributes;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Tabs;

public class InstancesTab(
    TextService TextService,
    DebugRenderer DebugRenderer,
    WindowManager WindowManager,
    InstancesService InstancesService,
    PinnedInstancesService PinnedInstances,
    ImGuiContextMenuService ImGuiContextMenu) : DebugTab
{
    private string SearchTerm = string.Empty;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref SearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(SearchTerm);

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        var i = 0;
        foreach (var (ptr, type) in InstancesService.Instances)
        {
            if (type.GetCustomAttribute<AgentAttribute>() != null) continue;
            if (hasSearchTerm && !type.FullName!.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) continue;

            DebugRenderer.DrawAddress(ptr);
            ImGui.SameLine(120);
            DebugRenderer.DrawPointerType(ptr, type, new NodeOptions()
            {
                AddressPath = new AddressPath(i++),
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    var isPinned = PinnedInstances.Contains(type);
                    var windowName = type.Name;

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !WindowManager.Contains(windowName),
                        Label = TextService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => WindowManager.Open(new PointerTypeWindow(WindowManager, DebugRenderer, ptr, type))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !isPinned,
                        Label = TextService.Translate("ContextMenu.PinnedInstances.Pin"),
                        ClickCallback = () => PinnedInstances.Add(ptr, type)
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = isPinned,
                        Label = TextService.Translate("ContextMenu.PinnedInstances.Unpin"),
                        ClickCallback = () => PinnedInstances.Remove(type)
                    });
                }
            });
        }
    }
}
