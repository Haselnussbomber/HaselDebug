using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Commands;
using HaselCommon.Services;
using HaselDebug.Config;
using HaselDebug.Windows;
using InteropGenerator.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug;

public class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ServiceProvider _serviceProvider;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ISigScanner sigScanner,
        IDataManager dataManager,
        IFramework framework)
    {
        _pluginInterface = pluginInterface;

        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();

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
