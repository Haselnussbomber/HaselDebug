using HaselDebug.Config;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Windows;

[RegisterSingleton, AutoConstruct]
public partial class ConfigWindow : SimpleWindow
{
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly PathList _pathList;

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

        ImGui.Separator();

        ImGui.Text("Path List Status:");
        ImGui.SameLine();
        switch (_pathList.Status)
        {
            case PathListStatus.NotLoaded:
                ImGui.Text("Not loaded"u8);
                break;
            case PathListStatus.Downloading:
                ImGui.Text("Downloading..."u8);
                break;
            case PathListStatus.Downloaded:
                ImGui.Text("Downloaded (Waiting to load)"u8);
                break;
            case PathListStatus.Loading:
                ImGui.Text("Loading..."u8);
                ImGuiUtilsEx.ProgressBar((float)_pathList.LoadProgress, new Vector2(-1, 0));
                break;
            case PathListStatus.Processing:
                ImGui.Text("Processing unknown paths..."u8);
                ImGuiUtilsEx.ProgressBar((float)(-1.0 * ImGui.GetTime()), new Vector2(-1, 0));
                break;
            case PathListStatus.Loaded:
                ImGui.Text($"Loaded ({_pathList.Count:N0} paths)");
                break;
            case PathListStatus.Error:
                ImGui.TextColored(Color.ErrorForeground, "Error loading path list"u8);
                break;
        }

        using (var workingDisabled = ImRaii.Disabled(_pathList.Status is PathListStatus.Downloading or PathListStatus.Loading))
        {
            using (var disabled = ImRaii.Disabled(!_pathList.IsCached))
            {
                if (ImGui.Button("Load Path List"))
                {
                    _ = _pathList.LoadPathList(false);
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Download Path List"))
            {
                _ = _pathList.LoadPathList(true);
            }
        }
    }
}
