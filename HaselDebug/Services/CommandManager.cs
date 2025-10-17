using System.Threading;
using System.Threading.Tasks;
using HaselCommon.Commands;
using HaselDebug.Config;
using HaselDebug.Windows;

namespace HaselDebug.Services;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class CommandManager : IHostedService
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly PluginConfig _pluginConfig;
    private readonly WindowManager _windowManager;
    private readonly CommandService _commandService;
    private CommandHandler? _commandHandler;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_pluginConfig.AutoOpenPluginWindow)
            _windowManager.CreateOrOpen<PluginWindow>();

        _pluginInterface.UiBuilder.Draw += DrawMainMenuItem;
        _pluginInterface.UiBuilder.OpenMainUi += TogglePluginWindow;
        _pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigWindow;

        _commandHandler = _commandService.Register(OnHaselDebugCommand, true);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.UiBuilder.Draw -= DrawMainMenuItem;
        _pluginInterface.UiBuilder.OpenMainUi -= TogglePluginWindow;
        _pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigWindow;

        _commandHandler?.Dispose();
        _commandHandler = null;

        return Task.CompletedTask;
    }

    private void DrawMainMenuItem()
    {
        if (_pluginInterface.IsDevMenuOpen && ImGui.BeginMainMenuBar())
        {
            if (ImGui.MenuItem("HaselDebug"))
            {
                TogglePluginWindow();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void TogglePluginWindow()
    {
        _windowManager.CreateOrToggle<PluginWindow>();
    }

    private void ToggleConfigWindow()
    {
        _windowManager.CreateOrToggle<ConfigWindow>();
    }

    [CommandHandler("/haseldebug", "HaselDebug.CommandHandlerHelpMessage")]
    private void OnHaselDebugCommand(string command, string arguments)
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
}
