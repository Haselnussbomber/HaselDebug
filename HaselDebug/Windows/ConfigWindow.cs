using System.Numerics;
using HaselCommon.Services;
using HaselCommon.Windowing;
using HaselDebug.Config;
using ImGuiNET;

namespace HaselDebug.Windows;

public class ConfigWindow : SimpleWindow
{
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    public ConfigWindow(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        TextService textService) : base(windowManager, textService.Translate("Config.WindowTitle"))
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        AllowClickthrough = false;
        AllowPinning = false;

        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        Size = new Vector2(380, -1);
        SizeCondition = ImGuiCond.Appearing;
    }

    public override void Draw()
    {
        var configChanged = false;

        // AutoOpenPluginWindow
        configChanged |= ImGui.Checkbox($"{TextService.Translate("Config.AutoOpenPluginWindow.Label")}##AutoOpenPluginWindow", ref PluginConfig.AutoOpenPluginWindow);

        if (configChanged)
            PluginConfig.Save();
    }
}
