using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Config;
using HaselDebug.Interfaces;
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

        Tabs = [.. tabs
            .Where(t => !t.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableTo(typeof(ISubTab<>)))) // no sub tabs
            .OrderBy(t => t.Title)
        ];

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
                        Visible = tab.CanPopOut && !WindowManager.Contains(tab.Title),
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

            using var disabled = Color.From(ImGuiCol.TextDisabled).Push(ImGuiCol.Text, !tab.IsEnabled);

            if (ImGui.Selectable($"{tab.Title}###Selectable_{tab.InternalName}", SelectedTab == tab))
            {
                SelectTab(tab);
            }

            disabled.Pop();

            ImGuiContextMenu.Draw($"{tab.InternalName}ContextMenu", builder =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = tab.CanPopOut && !WindowManager.Contains(tab.Title),
                    Label = TextService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => WindowManager.Open(new TabPopoutWindow(WindowManager, tab))
                });
            });

            if (tab.SubTabs != null)
            {
                var subTabCount = tab.SubTabs.Value.Length;
                for (var i = 0; i < subTabCount; i++)
                {
                    var subTab = tab.SubTabs.Value[i];

                    // pls don't make me handle nested subtabs
                    var prefix = "├";
                    if (i == subTabCount - 1)
                        prefix = "└";

                    using var subTabDisabled = Color.From(ImGuiCol.TextDisabled).Push(ImGuiCol.Text, !tab.IsEnabled || !subTab.IsEnabled);

                    if (ImGui.Selectable($"{prefix} {subTab.Title}###Selectable_{subTab.InternalName}", SelectedTab == subTab))
                    {
                        SelectTab(subTab);
                    }

                    subTabDisabled.Pop();

                    ImGuiContextMenu.Draw($"{subTab.InternalName}ContextMenu", builder =>
                    {
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = subTab.CanPopOut && !WindowManager.Contains(subTab.Title),
                            Label = TextService.Translate("ContextMenu.TabPopout"),
                            ClickCallback = () => WindowManager.Open(new TabPopoutWindow(WindowManager, subTab))
                        });
                    });
                }
            }
        }
    }

    private void SelectTab(IDrawableTab tab)
    {
        SelectedTab = tab;
        PluginConfig.LastSelectedTab = tab.InternalName;
        PluginConfig.Save();
    }

    private unsafe void DrawTab()
    {
        if (SelectedTab == null)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        using var child = SelectedTab.DrawInChild
            ? ImRaii.Child($"###{SelectedTab.InternalName}_Child", new Vector2(-1), true)
            : null;

        try
        {
            SelectedTab.Draw();
        }
        catch { }
    }
}
