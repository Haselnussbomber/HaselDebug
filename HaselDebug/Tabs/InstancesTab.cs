using System.Reflection;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InstancesTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly TypeService _typeService;
    private readonly PinnedInstancesService _pinnedInstances;

    private string _searchTerm = string.Empty;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_searchTerm);

        using var contentChild = ImRaii.Child("Content", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);

        foreach (var (i, type) in _typeService.Instances.Index())
        {
            if (_typeService.AgentTypes != null && _typeService.AgentTypes.ContainsValue(type))
                continue;

            if (hasSearchTerm && !type.FullName!.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                continue;

            var instanceMethod = type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public);
            if (instanceMethod == null)
                continue;

            var ptr = (Pointer?)instanceMethod.Invoke(null, null);
            if (ptr == null)
                continue;

            var address = (nint)Pointer.Unbox(ptr);

            if (address == 0)
            {
                _debugRenderer.DrawAddress(address);
                ImGui.SameLine(120);
                ImGui.TextColored(DebugRenderer.ColorTreeNode with { A = 0.5f }, type.FullName ?? "null");
                continue;
            }

            _debugRenderer.DrawAddress(address);
            ImGui.SameLine(120);
            _debugRenderer.DrawPointerType(address, type, new NodeOptions()
            {
                AddressPath = new AddressPath(i),
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    var isPinned = _pinnedInstances.Contains(type);
                    var windowName = type.Name;

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !_windowManager.Contains(win => win.WindowName == windowName),
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_windowManager, _textService, _serviceProvider, address, type, string.Empty))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                        ClickCallback = () => _pinnedInstances.Add(type)
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
