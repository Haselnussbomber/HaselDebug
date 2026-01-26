using HaselDebug.Config;
using HaselDebug.Interfaces;
using HaselDebug.Service;
using HaselDebug.Services;
using HaselDebug.Tabs;
using HaselDebug.Utils;

namespace HaselDebug.Windows;

[RegisterSingleton, AutoConstruct]
public partial class PluginWindow : SimpleWindow
{
    private const uint SidebarWidth = 250;

    private readonly ILogger<PluginWindow> _logger;
    private readonly WindowManager _windowManager;
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly AddonObserver _addonObserver;
    private readonly PinnedInstancesService _pinnedInstances;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly DebugRenderer _debugRenderer;
    private readonly ConfigWindow _configWindow;
    private readonly NavigationService _navigationService;
    private readonly ProcessInfoService _processInfoService;
    private readonly IEnumerable<IDebugTab> _debugTabs;

    private IDebugTab[] _tabs;
    private IDebugTab? _selectedTab;

    [AutoPostConstruct]
    public void Initialize()
    {
        Size = new Vector2(1440, 880);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(250, 250),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;

        TitleBarButtons.Add(new()
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new(0, 1),
            ShowTooltip = () =>
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.Text(_textService.Translate($"TitleBarButton.ToggleConfig.Tooltip.{(_configWindow.IsOpen ? "Close" : "Open")}Config"));
            },
            Click = (button) => _configWindow.Toggle()
        });

        _tabs = [.. _debugTabs
            //.Where(t => !t.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableTo(typeof(ISubTab<>)))) // no sub tabs
            .OrderBy(t => t.Title)
        ];

        _pinnedInstances.Loaded += OnPinnedInstancesLoaded;
    }

    public override void Dispose()
    {
        foreach (var tab in _tabs.OfType<IDisposable>())
            tab.Dispose();

        _processInfoService.Enabled = false;

        base.Dispose();
    }

    private void OnPinnedInstancesLoaded()
    {
        _pinnedInstances.Loaded -= OnPinnedInstancesLoaded;

        SelectTabWithoutSave(_pluginConfig.LastSelectedTab);

        if (_pluginConfig.AutoOpenPluginWindow)
            _windowManager.CreateOrOpen<PluginWindow>();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _debugRenderer.ParseCSDocs();
        _processInfoService.Enabled = true;
    }

    public override void OnClose()
    {
        base.OnClose();
        _processInfoService.Enabled = false;
    }

    public override bool DrawConditions()
    {
        return true;
    }

    public override void Draw()
    {
        HandleNavigation();

        DrawSidebar();
        ImGui.SameLine();
        DrawTab();

        _navigationService.PostDraw();
    }

    private void HandleNavigation()
    {
        switch (_navigationService.CurrentNavigation)
        {
            case AddonNavigation when _selectedTab is not AddonInspectorTab:
                SelectTab(nameof(AddonInspectorTab));
                break;
            case AgentNavigation when _selectedTab is not AgentsTab:
                SelectTab(nameof(AgentsTab));
                break;
            case AddressInspectorNavigation when _selectedTab is not AddressInspectorTab:
                SelectTab(nameof(AddressInspectorTab));
                break;
        }
    }

    private void DrawSidebar()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        var halfLineHeight = (int)MathF.Round(lineHeight / 2f);

        using var child = ImRaii.Child("Sidebar", new Vector2(SidebarWidth * scale, -1), true, ImGuiWindowFlags.NoSavedSettings);
        if (!child || !child.Success)
            return;

        using var table = ImRaii.Table("SidebarTable"u8, 1, ImGuiTableFlags.NoSavedSettings);
        if (!table || !table.Success)
            return;

        ImGui.TableSetupColumn("Tab Name"u8, ImGuiTableColumnFlags.WidthStretch);

        if (_pinnedInstances.Count > 0)
        {
            PinnedInstanceTab? removeTab = null;

            foreach (var tab in _pinnedInstances)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var selected = ImGui.Selectable($"{tab.Title}##Selectable_{tab.InternalName}", _selectedTab == tab);

                _imGuiContextMenu.Draw($"{tab.InternalName}ContextMenu", builder =>
                {
                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = tab.CanPopOut && !_windowManager.Contains(win => win.WindowName == tab.Title),
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => _windowManager.Open(new TabPopoutWindow(_windowManager, _textService, _addonObserver, tab))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                        ClickCallback = () => removeTab = tab
                    });
                });

                if (selected)
                {
                    _selectedTab = _selectedTab != tab ? tab : null;
                    _pluginConfig.LastSelectedTab = _selectedTab != null ? tab.InternalName : string.Empty;
                    _pluginConfig.Save();
                }
            }

            if (removeTab != null)
            {
                _selectedTab = null;
                _pluginConfig.LastSelectedTab = string.Empty;
                _pinnedInstances.Remove(removeTab);
            }

            ImGui.Separator();
        }

        foreach (var tab in _tabs)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            using var disabled = Color.From(ImGuiCol.TextDisabled).Push(ImGuiCol.Text, !tab.IsEnabled);

            if (ImGui.Selectable($"{tab.Title}###Selectable_{tab.InternalName}", _selectedTab == tab))
            {
                SelectTab(tab);
            }

            disabled.Pop();

            _imGuiContextMenu.Draw($"{tab.InternalName}ContextMenu", builder =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = tab.CanPopOut && !_windowManager.Contains(win => win.WindowName == tab.Title),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => _windowManager.Open(new TabPopoutWindow(_windowManager, _textService, _addonObserver, tab))
                });
            });

            if (tab.SubTabs != null)
            {
                var subTabCount = tab.SubTabs.Value.Length;
                for (var i = 0; i < subTabCount; i++)
                {
                    var subTab = tab.SubTabs.Value[i];

                    using var subTabDisabled = Color.From(ImGuiCol.TextDisabled).Push(ImGuiCol.Text, !tab.IsEnabled || !subTab.IsEnabled);
                    var pos = ImGui.GetCursorPos();

                    if (ImGui.Selectable($"###Selectable_{subTab.InternalName}", _selectedTab == subTab))
                    {
                        SelectTab(subTab);
                    }

                    subTabDisabled.Pop();

                    _imGuiContextMenu.Draw($"{subTab.InternalName}ContextMenu", builder =>
                    {
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = subTab.CanPopOut && !_windowManager.Contains(win => win.WindowName == subTab.Title),
                            Label = _textService.Translate("ContextMenu.TabPopout"),
                            ClickCallback = () => _windowManager.Open(new TabPopoutWindow(_windowManager, _textService, _addonObserver, subTab))
                        });
                    });

                    ImGui.SameLine(0, lineHeight);
                    ImGui.Text(subTab.Title);

                    var linePos = ImGui.GetWindowPos() + pos
                        - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY())
                        + new Vector2(MathF.Round(halfLineHeight - ImGui.GetStyle().ItemSpacing.Y / 2), -MathF.Round(ImGui.GetStyle().ItemSpacing.Y / 2));

                    ImGui.GetWindowDrawList().AddLine(linePos, linePos + new Vector2(0, i == subTabCount - 1 ? halfLineHeight : lineHeight), Color.Grey3.ToUInt());
                    ImGui.GetWindowDrawList().AddLine(linePos + new Vector2(0, halfLineHeight), linePos + new Vector2(halfLineHeight, halfLineHeight), Color.Grey3.ToUInt());
                }
            }
        }
    }

    public void SelectTab(string internalName)
    {
        SelectTabWithoutSave(internalName);
        _pluginConfig.LastSelectedTab = _selectedTab?.InternalName ?? string.Empty;
        _pluginConfig.Save();
    }

    private void SelectTabWithoutSave(string internalName)
    {
        _selectedTab = _pinnedInstances.FirstOrDefault(tab => tab.InternalName == internalName)
            ?? _tabs.FirstOrDefault(tab => tab.InternalName == internalName)
            ?? _tabs
                .Where(tab => tab.SubTabs?.Any(subTab => subTab.InternalName == internalName) == true)
                .Select(tab => tab.SubTabs?.FirstOrDefault(subTab => subTab.InternalName == internalName))
                .FirstOrDefault();
    }

    private void SelectTab(IDebugTab tab)
    {
        _selectedTab = tab;
        _pluginConfig.LastSelectedTab = tab.InternalName;
        _pluginConfig.Save();
    }

    private unsafe void DrawTab()
    {
        if (_selectedTab == null)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        using var child = _selectedTab.DrawInChild
            ? ImRaii.Child($"###{_selectedTab.InternalName}_Child", new Vector2(-1), true)
            : null;

        try
        {
            _selectedTab.Draw();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while drawing {tabName}", _selectedTab.InternalName);
        }
    }
}
