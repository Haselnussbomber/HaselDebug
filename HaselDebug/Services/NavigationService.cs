using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;
using HaselDebug.Service;
using HaselDebug.Windows;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class NavigationService
{
    private readonly TypeService _typeService;
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProcessInfoService _processInfoService;

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

        ImGuiContextMenu.Draw($"AgentNavigationContextMenu{_tooltipIndex++}", (builder) =>
        {
            var agentType = _typeService.GetAgentType(agentId);
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
                    var window = _serviceProvider.CreateInstance<PointerTypeWindow>((nint)agent, agentType, agentName);
                    window.WindowName = displayName;
                    _windowManager.Open(window);
                }
            });

            var pinnedInstancesService = _serviceProvider.GetRequiredService<PinnedInstancesService>();
            var isPinned = pinnedInstancesService.Contains(agentType);

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = !isPinned,
                Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                ClickCallback = () => pinnedInstancesService.Add(agentType)
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = isPinned,
                Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                ClickCallback = () => pinnedInstancesService.Remove(agentType)
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

        ImGuiContextMenu.Draw($"AddonNavigationContextMenu{_tooltipIndex++}", (builder) =>
        {
            var type = _typeService.GetAddonType(addonName);

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
                    var window = _serviceProvider.CreateInstance<AddonInspectorWindow>();
                    window.AddonId = addonId;
                    window.AddonName = addonName;
                    window.WindowName = displayName;
                    _windowManager.Open(window);
                }
            });
        });
    }

    public void DrawAddressInspectorLink(nint address, uint size = 0)
    {
        if (address == 0)
        {
            ImGui.Text("null");
            return;
        }

        var displayText = ImGui.IsKeyDown(ImGuiKey.LeftShift)
            ? $"0x{address:X}"
            : _processInfoService.GetAddressName(address);

        ImGuiUtils.DrawCopyableText(displayText);

        ImGuiContextMenu.Draw($"Address_AddressInspectorNavigation_ContextMenu{_tooltipIndex++}", (builder) =>
        {
            builder.AddCopyAddress(address);
            if (displayText != $"0x{address:X}")
                builder.AddCopyValueString(displayText);

            builder.AddSeparator();

            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = _textService.Translate("ContextMenu.GoToAddressInspector"),
                ClickCallback = () => CurrentNavigation = new AddressInspectorNavigation(address, size)
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
    public List<Pointer<AtkResNode>>? NodePath { get; init; }
}

public readonly struct AgentNavigation(AgentId agentId) : INavigationParams
{
    public AgentId AgentId { get; init; } = agentId;
}

public readonly struct AddressInspectorNavigation(nint address, uint size = 0) : INavigationParams
{
    public nint Address { get; init; } = address;
    public uint Size { get; init; } = size;
}
