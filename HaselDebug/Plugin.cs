using Dalamud.Plugin;
using HaselDebug.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HaselDebug;

public class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IHost _host;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        _pluginInterface.InitializeCustomClientStructs();

        _host = new HostBuilder()
            .UseContentRoot(pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(pluginInterface);
                services.AddSingleton(PluginConfig.Load);
                services.AddHaselCommon();
                services.AddHaselDebug();
            })
            .Build();

        _host.Start();
    }

    void IDisposable.Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
