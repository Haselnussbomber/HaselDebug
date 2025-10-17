using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;
using HaselDebug.Windows;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class NavigationService
{
    private readonly TypeService _typeService;
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ImGuiContextMenuService _imGuiContextMenu;

    private int _tooltipIndex;

    public INavigationParams? CurrentNavigation { get; private set; }

    public void NavigateTo(INavigationParams navParams)
    {
        CurrentNavigation = navParams;
    }

    public void DrawAgentLink(AgentId agentId)
    {
        var displayName = "Agent" + agentId.ToString();

        ImGui.TextColored(Color.Gold, displayName);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip($"Go to {displayName}");
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            CurrentNavigation = new AgentNavigation(agentId);
        }

        _imGuiContextMenu.Draw($"AgentNavigationContextMenu{_tooltipIndex++}", (builder) =>
        {
            if (!_typeService.AgentTypes.TryGetValue(agentId, out var type))
                type = typeof(AgentInterface);

            var agentName = agentId.ToString();
            var agent = AgentModule.Instance()->GetAgentByInternalId(agentId);

            builder.AddCopyName(agentName);
            builder.AddCopyAddress((nint)agent);

            builder.AddSeparator();

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = !_windowManager.Contains(win => win.WindowName == displayName),
                Label = _textService.Translate("ContextMenu.TabPopout"),
                ClickCallback = () =>
                {
                    var window = ActivatorUtilities.CreateInstance<PointerTypeWindow>(_serviceProvider, (nint)agent, type, agentName);
                    window.WindowName = displayName;
                    _windowManager.Open(window);
                }
            });

            var pinnedInstancesService = _serviceProvider.GetRequiredService<PinnedInstancesService>();
            var isPinned = pinnedInstancesService.Contains(type);

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = !isPinned,
                Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                ClickCallback = () => pinnedInstancesService.Add((nint)agent, type)
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = isPinned,
                Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                ClickCallback = () => pinnedInstancesService.Remove(type)
            });
        });
    }

    public void DrawAddonLink(ushort addonId, string addonName)
    {
        var displayName = "Addon" + addonName;

        ImGui.TextColored(Color.Gold, displayName);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip($"Go to {displayName}");
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            CurrentNavigation = new AddonNavigation(addonId, addonName);
        }

        _imGuiContextMenu.Draw($"AddonNavigationContextMenu{_tooltipIndex++}", (builder) =>
        {
            if (!_typeService.AddonTypes.TryGetValue(addonName, out var type))
                type = typeof(AtkUnitBase);

            var unitBase = RaptureAtkUnitManager.Instance()->GetAddonById(addonId);
            if (unitBase == null)
                unitBase = RaptureAtkUnitManager.Instance()->GetAddonByName(addonName);

            builder.AddCopyName(addonName);
            builder.AddCopyAddress((nint)unitBase);

            builder.AddSeparator();

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = !_windowManager.Contains(win => win.WindowName == displayName),
                Label = _textService.Translate("ContextMenu.TabPopout"),
                ClickCallback = () =>
                {
                    var window = ActivatorUtilities.CreateInstance<AddonInspectorWindow>(_serviceProvider);
                    window.AddonId = addonId;
                    window.AddonName = addonName;
                    window.WindowName = displayName;
                    _windowManager.Open(window);
                }
            });
        });
    }

    public void Reset()
    {
        CurrentNavigation = null;
    }

    public void PostDraw()
    {
        _tooltipIndex = 0;
    }
}

public interface INavigationParams;

public readonly struct AddonNavigation(ushort addonId, string? addonName) : INavigationParams
{
    public ushort AddonId { get; init; } = addonId;
    public string? AddonName { get; init; } = addonName;
}

public readonly struct AgentNavigation(AgentId agentId) : INavigationParams
{
    public AgentId AgentId { get; init; } = agentId;
}
