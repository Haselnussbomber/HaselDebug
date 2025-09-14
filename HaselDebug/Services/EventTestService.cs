using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using R3;

namespace HaselCommon.Services;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EventTestService : IHostedService
{
    private readonly ILogger<EventTestService> _logger;
    private readonly IFramework _framework;
    private readonly IGameConfig _gameConfig;
    private IDisposable _disposable;

    public Task StartAsync(CancellationToken token)
    {
        _logger.LogDebug("Starting EventTestService");

        var builder = Disposable.CreateBuilder();
        /*
        new ObservedValue<byte>(() => PlayerState.Instance()->CurrentClassJobId)
            .Subscribe(id => _logger.LogTrace("ClassJobId changed: {c}", id))
            .AddTo(ref builder);
        
        Observable.FromEventHandler<ConfigChangeEvent>(
            a => _gameConfig.Changed += a,
            a => _gameConfig.Changed -= a,
            token)
            .Subscribe(x => _logger.LogTrace("Config changed: {c}", x.e.Option))
            .AddTo(ref builder);

        var eachTick = Observable.EveryUpdate(token).Share();

        var classJobId = eachTick
            .Select(_ => PlayerState.Instance()->CurrentClassJobId)
            .DistinctUntilChanged()
            .ToReadOnlyReactiveProperty()
            .Subscribe(id => _logger.LogTrace("ClassJobId changed: {c}", id))
            .AddTo(ref builder);

        Observable
            .IntervalFrame(30, token)
            .Subscribe(_ => _logger.LogTrace("EventTestService IntervalFrame. IsOnFrameworkThread: {ioft}", ThreadSafety.IsMainThread))
            .AddTo(ref builder);

        Observable
            .Interval(TimeSpan.FromSeconds(1), token)
            .Subscribe(_ => _logger.LogTrace("EventTestService Interval. IsOnFrameworkThread: {ioft}", ThreadSafety.IsMainThread))
            .AddTo(ref builder);

        Observable
            .Interval(TimeSpan.FromSeconds(1), token)
            .SubscribeOnFrameworkThread(_framework)
            .Subscribe(_ => _logger.LogTrace("EventTestService Interval Framework. IsOnFrameworkThread: {ioft}", ThreadSafety.IsMainThread))
            .AddTo(ref builder);
        */
        _disposable = builder.Build();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken _)
    {
        _disposable.Dispose();
        return Task.CompletedTask;
    }
}
