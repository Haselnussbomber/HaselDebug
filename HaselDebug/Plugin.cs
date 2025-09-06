using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Commands;
using HaselCommon.Services;
using HaselDebug.Config;
using HaselDebug.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug;

public class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ServiceProvider _serviceProvider;

    public Plugin(IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        _pluginInterface = pluginInterface;
        _pluginInterface.InitializeCustomClientStructs();

        _serviceProvider = new ServiceCollection()
            .AddDalamud(pluginInterface)
            .AddSingleton(PluginConfig.Load)
            .AddHaselCommon()
            .AddHaselDebug()
            .BuildServiceProvider();

        framework.RunOnFrameworkThread(() =>
        {
            if (_serviceProvider.GetRequiredService<PluginConfig>().AutoOpenPluginWindow)
                _serviceProvider.GetRequiredService<PluginWindow>().Open();

            _serviceProvider.GetRequiredService<CommandService>().Register(OnCommand, true);

            _pluginInterface.UiBuilder.Draw += DrawMainMenuItem;
            _pluginInterface.UiBuilder.OpenMainUi += TogglePluginWindow;
            _pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;
        });
    }

    [CommandHandler("/haseldebug", "HaselDebug.CommandHandlerHelpMessage")]
    private void OnCommand(string command, string arguments)
    {
        switch (arguments.Trim().ToLowerInvariant())
        {
            case "conf":
            case "config":
                ToggleConfigWindow();
                break;

            default:
                TogglePluginWindow();
                break;
        }
    }

    private void DrawMainMenuItem()
    {
        if (_pluginInterface.IsDevMenuOpen && ImGui.BeginMainMenuBar())
        {
            if (ImGui.MenuItem("HaselDebug"))
            {
                _serviceProvider.GetRequiredService<PluginWindow>().Toggle();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void TogglePluginWindow()
    {
        _serviceProvider.GetRequiredService<PluginWindow>().Toggle();
    }

    private void ToggleConfigWindow()
    {
        _serviceProvider.GetRequiredService<ConfigWindow>().Toggle();
    }

    void IDisposable.Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= DrawMainMenuItem;
        _pluginInterface.UiBuilder.OpenMainUi -= TogglePluginWindow;
        _pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;

        _serviceProvider.Dispose();
    }
}
