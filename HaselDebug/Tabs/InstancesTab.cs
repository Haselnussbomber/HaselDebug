using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Attributes;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class InstancesTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly InstancesService _instancesService;
    private readonly PinnedInstancesService _pinnedInstances;

    private string _searchTerm = string.Empty;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_searchTerm);

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        var i = 0;
        foreach (var (ptr, type) in _instancesService.Instances)
        {
            if (type.GetCustomAttribute<AgentAttribute>() != null) continue;
            if (hasSearchTerm && !type.FullName!.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)) continue;

            _debugRenderer.DrawAddress(ptr);
            ImGui.SameLine(120);
            _debugRenderer.DrawPointerType(ptr, type, new NodeOptions()
            {
                AddressPath = new AddressPath(i++),
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    var isPinned = _pinnedInstances.Contains(type);
                    var windowName = type.Name;

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !_windowManager.Contains(win => win.WindowName == windowName),
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_serviceProvider, ptr, type, string.Empty))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                        ClickCallback = () => _pinnedInstances.Add(ptr, type)
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                        ClickCallback = () => _pinnedInstances.Remove(type)
                    });
                }
            });
        }
    }
}
