using System.Numerics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Config;
using ImGuiNET;

namespace HaselDebug.Windows;

[RegisterSingleton]
public class ConfigWindow : SimpleWindow
{
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    public ConfigWindow(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        TextService textService,
        LanguageProvider languageProvider) : base(windowManager, textService, languageProvider)
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        AllowClickthrough = false;
        AllowPinning = false;

        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        Size = new Vector2(380, -1);
        SizeCondition = ImGuiCond.Appearing;
    }

    public override bool DrawConditions()
    {
        return true;
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
