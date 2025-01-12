using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon;
using HaselCommon.Commands;
using HaselCommon.Services;
using HaselDebug.Config;
using HaselDebug.Windows;
using ImGuiNET;
using InteropGenerator.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug;

public class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IDataManager dataManager,
        IClientState clientState)
    {
        _pluginInterface = pluginInterface;

#if HAS_LOCAL_CS
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();
#endif

        Service.Collection
            .AddDalamud(pluginInterface)
            .AddSingleton(PluginConfig.Load)
            .AddHaselCommon()
            .AddHaselDebug();

        Service.BuildProvider();

        framework.RunOnFrameworkThread(() =>
        {
            if (Service.Get<PluginConfig>().AutoOpenPluginWindow)
                Service.Get<PluginWindow>().Open();

            Service.Get<CommandService>().Register(OnCommand, true);

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
                Service.Get<PluginWindow>().Toggle();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private static void TogglePluginWindow()
    {
        Service.Get<PluginWindow>().Toggle();
    }

    private static void ToggleConfigWindow()
    {
        Service.Get<ConfigWindow>().Toggle();
    }

    void IDisposable.Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= DrawMainMenuItem;
        _pluginInterface.UiBuilder.OpenMainUi -= TogglePluginWindow;
        _pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;

        Service.Dispose();
    }
}
