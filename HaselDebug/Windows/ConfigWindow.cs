using HaselDebug.Config;

namespace HaselDebug.Windows;

[RegisterSingleton, AutoConstruct]
public partial class ConfigWindow : SimpleWindow
{
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;

    [AutoPostConstruct]
    public void Initialize()
    {
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
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.AutoOpenPluginWindow.Label")}##AutoOpenPluginWindow", ref _pluginConfig.AutoOpenPluginWindow);

        if (configChanged)
            _pluginConfig.Save();
    }
}
