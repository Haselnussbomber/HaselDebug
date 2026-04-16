using HaselDebug.Config;
using HaselDebug.Services;

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

        // ShowInDevMenu
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.ShowInDevMenu.Label")}##ShowInDevMenu", ref _pluginConfig.ShowInDevMenu);

        using (ImGuiUtils.ConfigIndent())
            ImGui.TextColoredWrapped(Color.Text600, _textService.Translate("Config.ShowInDevMenu.Description"));

        // AutoOpenPluginWindow
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.AutoOpenPluginWindow.Label")}##AutoOpenPluginWindow", ref _pluginConfig.AutoOpenPluginWindow);

        // EnableLuaLogger
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.EnableLuaLogger.Label")}##EnableLuaLogger", ref _pluginConfig.EnableLuaLogger);

        using (ImGuiUtils.ConfigIndent())
            ImGui.TextColoredWrapped(Color.Text600, _textService.Translate("Config.EnableLuaLogger.Description"));

        // ResolveAddonLifecycleVTables
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.ResolveAddonLifecycleVTables.Label")}##ResolveAddonLifecycleVTables", ref _pluginConfig.ResolveAddonLifecycleVTables);

        // ResolveAgentLifecycleVTables
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.ResolveAgentLifecycleVTables.Label")}##ResolveAgentLifecycleVTables", ref _pluginConfig.ResolveAgentLifecycleVTables);

        // SpacesInKTKNames
        configChanged |= ImGui.Checkbox($"{_textService.Translate("Config.SpacesInKTKNames.Label")}##SpacesInKTKNames", ref _pluginConfig.SpacesInKTKNames);

        using (ImGuiUtils.ConfigIndent())
            ImGui.TextColoredWrapped(Color.Text600, _textService.Translate("Config.SpacesInKTKNames.Description"));

        if (configChanged)
        {
            _pluginConfig.Save();

            if (ServiceLocator.TryGetService<LuaLogger>(out var luaLogger))
            {
                if (_pluginConfig.EnableLuaLogger)
                    luaLogger.Enable();
                else
                    luaLogger.Disable();
            }
        }
    }
}
