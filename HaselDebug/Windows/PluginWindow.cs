using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Services;
using HaselCommon.Windowing;
using HaselDebug.Abstracts;
using HaselDebug.Config;
using ImGuiNET;

namespace HaselDebug.Windows;

public class PluginWindow : SimpleWindow, IDisposable
{
    private const uint SidebarWidth = 250;
    private readonly IDebugTab[] Tabs;

    private readonly PluginConfig PluginConfig;
    private IDebugTab? SelectedTab;

    public PluginWindow(
        PluginConfig pluginConfig,
        WindowManager windowManager,
        IEnumerable<IDebugTab> tabs,
        TextService textService,
        ConfigWindow configWindow)
        : base(windowManager, "HaselDebug")
    {
        PluginConfig = pluginConfig;

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

        Tabs = [.. tabs.OrderBy(t => t.GetTitle())];

        SelectedTab = Tabs.FirstOrDefault(tab => tab.GetType().Name == pluginConfig.LastSelectedTab);
    }

    public new void Dispose()
    {
        foreach (var tab in Tabs)
        {
            (tab as IDisposable)?.Dispose();
        }

        base.Dispose();
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
        using var child = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth * scale, -1), true);
        if (!child || !child.Success)
            return;

        using var table = ImRaii.Table("##SidebarTable", 1, ImGuiTableFlags.NoSavedSettings);
        if (!table || !table.Success)
            return;

        ImGui.TableSetupColumn("Debug Tab Name", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tab in Tabs)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Selectable($"{tab.GetTitle()}##Selectable_{tab.GetType().Name}", SelectedTab == tab))
            {
                SelectedTab = SelectedTab != tab ? tab : null;
                PluginConfig.LastSelectedTab = SelectedTab != null ? tab.GetType().Name : string.Empty;
                PluginConfig.Save();
            }
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

        using var id = ImRaii.PushId(SelectedTab.GetType().Name);
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
