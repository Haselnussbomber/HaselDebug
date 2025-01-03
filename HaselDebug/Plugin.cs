using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Commands;
using HaselCommon.Extensions.DependencyInjection;
using HaselCommon.Logger;
using HaselCommon.Services;
using HaselDebug.Config;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;
using InteropGenerator.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HaselDebug;

public class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface PluginInterface;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IDataManager dataManager,
        IClientState clientState)
    {
        PluginInterface = pluginInterface;

#if HAS_LOCAL_CS
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();
#endif

        Service
            // Dalamud & HaselCommon
            .Initialize(pluginInterface, pluginLog)

            // Logging
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new DalamudLoggerProvider(pluginLog));
            })

            .AddSingleton(dataManager.Excel)

            // HaselDebug
            .AddSingleton(PluginConfig.Load(pluginInterface, pluginLog))
            .AddSingleton<DebugRenderer>()
            .AddSingleton<InstancesService>()
            .AddSingleton<PinnedInstancesService>()
            .AddSingleton<UnlocksTabUtils>()
            .AddIServices<IDebugTab>()
            .AddSubTabs()
            .AddSingleton<PluginWindow>()
            .AddSingleton<ConfigWindow>();

        Service.BuildProvider();

        // TODO: IHostedService?
        framework.RunOnFrameworkThread(() =>
        {
            if (Service.Get<PluginConfig>().AutoOpenPluginWindow)
                Service.Get<PluginWindow>().Open();

            Service.Get<CommandService>().Register(OnCommand, true);

            PluginInterface.UiBuilder.Draw += DrawMainMenuItem;
            PluginInterface.UiBuilder.OpenMainUi += TogglePluginWindow;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;
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
        if (PluginInterface.IsDevMenuOpen && ImGui.BeginMainMenuBar())
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
        PluginInterface.UiBuilder.Draw -= DrawMainMenuItem;
        PluginInterface.UiBuilder.OpenMainUi -= TogglePluginWindow;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;

        Service.Dispose();
    }
}
