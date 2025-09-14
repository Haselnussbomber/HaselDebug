using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using Microsoft.Extensions.Logging;
using R3;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EventTestTab : DebugTab, IDisposable
{
    private readonly ILogger<EventTestTab> _logger;
    private bool _initialized;
    private IDisposable? _disposable;
    private readonly ReactivePlayerState _playerState;

    private void Initialize()
    {
        _disposable = _playerState.ClassJobId
            .Subscribe(OnClassJobIdChanged);
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        ImGui.Text($"ClassJobId: {_playerState.ClassJobId.CurrentValue}");
    }

    private void OnClassJobIdChanged(byte id)
    {
        _logger.LogInformation("ClassJobId changed: {c}", id);
    }

    public void Dispose()
    {
        _disposable?.Dispose();
    }
}
