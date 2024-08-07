using System.IO;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Commands;
using HaselCommon.Extensions;
using HaselCommon.Logger;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Config;
using HaselDebug.Windows;
using InteropGenerator.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HaselDebug;

public class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface PluginInterface;
    private PluginWindow? PluginWindow;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog pluginLog,
        ISigScanner sigScanner,
        IDataManager dataManager)
    {
        PluginInterface = pluginInterface;

        Service
            // Dalamud & HaselCommon
            .Initialize(pluginInterface)

            // Logging
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new DalamudLoggerProvider(pluginLog));
            })

            // HaselDebug
            .AddSingleton(PluginConfig.Load(pluginInterface, pluginLog))
            .AddIServices<IDebugTab>()
            .AddSingleton<PluginWindow>();

        Service.BuildProvider();

        // ---

#if HAS_LOCAL_CS
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        //Addresses.Register();
        Resolver.GetInstance.Setup(
            sigScanner.SearchBase,
            dataManager.GameData.Repositories["ffxiv"].Version,
            new FileInfo(Path.Join(pluginInterface.ConfigDirectory.FullName, "SigCache.json")));
        Resolver.GetInstance.Resolve();
#endif

        // ---

        // TODO: IHostedService?
        framework.RunOnFrameworkThread(() => {
            PluginWindow = Service.Get<PluginWindow>();
            PluginWindow.Open();

            Service.Get<CommandService>().Register(OnCommand);

            PluginInterface.UiBuilder.OpenMainUi += ToggleWindow;
        });
    }

    [CommandHandler("/haseldebug", "HaselDebug.CommandHandlerHelpMessage")]
    private void OnCommand(string command, string arguments)
    {
        ToggleWindow();
    }

    private static void ToggleWindow()
    {
        Service.Get<PluginWindow>().Toggle();
    }

    void IDisposable.Dispose()
    {
        PluginInterface.UiBuilder.OpenMainUi -= ToggleWindow;

        Service.Dispose();

#if HAS_LOCAL_CS
        //Addresses.Unregister();
#endif
    }
}
