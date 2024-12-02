using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Config;
using HaselDebug.Services;
using HaselDebug.Tabs;
using ImGuiNET;

namespace HaselDebug.Windows;

public class PluginWindow : SimpleWindow
{
    private const uint SidebarWidth = 250;
    private readonly IDebugTab[] Tabs;

    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;
    private readonly PinnedInstancesService PinnedInstances;
    private readonly ImGuiContextMenuService ImGuiContextMenu;
    private readonly DebugRenderer DebugRenderer;
    private IDrawableTab? SelectedTab;

    public PluginWindow(
        PluginConfig pluginConfig,
        WindowManager windowManager,
        IEnumerable<IDebugTab> tabs,
        TextService textService,
        ConfigWindow configWindow,
        PinnedInstancesService pinnedInstances,
        ImGuiContextMenuService imGuiContextMenuService,
        DebugRenderer debugRenderer)
        : base(windowManager, "HaselDebug")
    {
        PluginConfig = pluginConfig;
        TextService = textService;
        PinnedInstances = pinnedInstances;
        ImGuiContextMenu = imGuiContextMenuService;
        DebugRenderer = debugRenderer;

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
                textService.Draw($"TitleBarButton.ToggleConfig.Tooltip.{(configWindow.IsOpen ? "Close" : "Open")}Config");
            },
            Click = (button) => configWindow.Toggle()
        });

        Tabs = [.. tabs.OrderBy(t => t.Title)];

        SelectedTab = PinnedInstances.FirstOrDefault(tab => tab.InternalName == pluginConfig.LastSelectedTab)
            ?? (IDrawableTab?)Tabs.FirstOrDefault(tab => tab.InternalName == pluginConfig.LastSelectedTab);
    }

    public override void Dispose()
    {
        foreach (var tab in Tabs.OfType<IDisposable>())
            tab.Dispose();

        base.Dispose();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        DebugRenderer.ParseCSDocs();
    }

    public override void Draw()
    {
        DrawSidebar();
        ImGui.SameLine();
        DrawTab();
    }

    private void DrawSidebar()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using var child = ImRaii.Child("Sidebar", new Vector2(SidebarWidth * scale, -1), true, ImGuiWindowFlags.NoSavedSettings);
        if (!child || !child.Success)
            return;

        using var table = ImRaii.Table("SidebarTable", 1, ImGuiTableFlags.NoSavedSettings);
        if (!table || !table.Success)
            return;

        ImGui.TableSetupColumn("Tab Name", ImGuiTableColumnFlags.WidthStretch);

        if (PinnedInstances.Count > 0)
        {
            PinnedInstanceTab? removeTab = null;

            foreach (var tab in PinnedInstances)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var selected = ImGui.Selectable($"{tab.Title}##Selectable_{tab.InternalName}", SelectedTab == tab);

                ImGuiContextMenu.Draw($"{tab.InternalName}ContextMenu", builder =>
                {
                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !WindowManager.Contains(tab.Title),
                        Label = TextService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => WindowManager.Open(new TabPopoutWindow(WindowManager, tab))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Label = TextService.Translate("ContextMenu.PinnedInstances.Unpin"),
                        ClickCallback = () => removeTab = tab
                    });
                });

                if (selected)
                {
                    SelectedTab = SelectedTab != tab ? tab : null;
                    PluginConfig.LastSelectedTab = SelectedTab != null ? tab.InternalName : string.Empty;
                    PluginConfig.Save();
                }
            }

            if (removeTab != null)
            {
                SelectedTab = null;
                PluginConfig.LastSelectedTab = string.Empty;
                PinnedInstances.Remove(removeTab);
            }

            ImGui.Separator();
        }

        foreach (var tab in Tabs)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Selectable($"{tab.Title}##Selectable_{tab.InternalName}", SelectedTab == tab))
            {
                SelectedTab = SelectedTab != tab ? tab : null;
                PluginConfig.LastSelectedTab = SelectedTab != null ? tab.InternalName : string.Empty;
                PluginConfig.Save();
            }

            ImGuiContextMenu.Draw($"{tab.InternalName}ContextMenu", builder =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !WindowManager.Contains(tab.Title),
                    Label = TextService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => WindowManager.Open(new TabPopoutWindow(WindowManager, tab))
                });
            });
        }
    }

    private unsafe void DrawTab()
    {
        if (SelectedTab == null)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        var child = SelectedTab.DrawInChild
            ? ImRaii.Child("##Tab", new Vector2(-1), true)
            : null;

        using var id = ImRaii.PushId(SelectedTab.InternalName);
        try
        {
            SelectedTab.Draw();
        }
        finally
        {
            child?.Dispose();
        }
    }
}
