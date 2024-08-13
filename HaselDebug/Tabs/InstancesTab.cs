using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FFXIVClientStructs.Attributes;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public class InstancesTab(
    TextService TextService,
    DebugRenderer DebugRenderer,
    InstancesService InstancesService,
    PinnedInstancesService PinnedInstances,
    ImGuiContextMenuService ImGuiContextMenu) : DebugTab
{
    public override void Draw()
    {
        var i = 0;
        foreach (var (ptr, type) in InstancesService.Instances)
        {
            if (type.GetCustomAttribute<AgentAttribute>() != null) continue;

            DebugRenderer.DrawAddress(ptr);
            ImGui.SameLine(120);
            DebugRenderer.DrawPointerType(ptr, type, new NodeOptions() {
                AddressPath = new AddressPath(i++),
                DrawContextMenu = (nodeOptions) =>
                {
                    ImGuiContextMenu.Draw($"ContextMenu{nodeOptions.AddressPath}", builder =>
                    {
                        var isPinned = PinnedInstances.Contains(type);

                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = !isPinned,
                            Label = TextService.Translate("PinnedInstances.Pin"),
                            ClickCallback = () => PinnedInstances.Add(ptr, type)
                        });

                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = isPinned,
                            Label = TextService.Translate("PinnedInstances.Unpin"),
                            ClickCallback = () => PinnedInstances.Remove(type)
                        });
                    });
                }
            });
        }
    }
}
