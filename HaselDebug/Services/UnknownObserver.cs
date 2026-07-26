using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Game;
using HaselDebug.Config;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;

namespace HaselDebug.Services;

[RegisterSingleton, RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class UnknownObserver : IHostedService, IDisposable
{
    private readonly ILogger<UnknownObserver> _logger;
    private readonly PluginConfig _pluginConfig;
    private readonly IGameInteropProvider _gameInteropProvider;

    private readonly Dictionary<(byte, byte), bool> _subscribeAtkArrayLog = [];
    private readonly Dictionary<EventType, bool> _dispatchEventLog = [];
    private Hook<AtkUnitBase.Delegates.SubscribeAtkArrayData>? _subscribeAtkArrayDataHook;
    private Hook<EventHandler.Delegates.DispatchEvent>? _dispatchEventHook;

    [AutoPostConstruct]
    private void Initialize()
    {
        _subscribeAtkArrayDataHook = _gameInteropProvider.HookFromAddress<AtkUnitBase.Delegates.SubscribeAtkArrayData>(AtkUnitBase.MemberFunctionPointers.SubscribeAtkArrayData, SubscribeAtkArrayDataDetour);
        _dispatchEventHook = _gameInteropProvider.HookFromAddress<EventHandler.Delegates.DispatchEvent>(EventHandler.MemberFunctionPointers.DispatchEvent, DispatchEventDetour);

        if (_pluginConfig.EnableUnknownObserver)
            Enable();
    }

    public void Dispose()
    {
        Disable();
        _subscribeAtkArrayDataHook?.Dispose();
        _dispatchEventHook?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Enable()
    {
        _subscribeAtkArrayDataHook?.Enable();
        // _dispatchEventHook?.Enable(); // don't like that rn
        _logger.LogInformation("Enabled");
    }

    public void Disable()
    {
        _subscribeAtkArrayDataHook?.Disable();
        _dispatchEventHook?.Disable();
        _logger.LogInformation("Disabled");
    }

    private void SubscribeAtkArrayDataDetour(AtkUnitBase* thisPtr, byte arrayType, byte arrayIndex)
    {
        _logger.LogDebug("SubscribeAtkArrayDataDetour({arrayType}, {arrayIndex})", arrayType, arrayIndex);

        switch (arrayType)
        {
            case 0 when !Enum.IsDefined(typeof(StringArrayType), (int)arrayIndex) && !_subscribeAtkArrayLog.ContainsKey((arrayType, arrayIndex)):
                Print($"Unknown StringArray #{arrayIndex} subscribed by {thisPtr->NameString}");
                _subscribeAtkArrayLog.TryAdd((arrayType, arrayIndex), true);
                break;

            case 1 when !Enum.IsDefined(typeof(NumberArrayType), (int)arrayIndex) && !_subscribeAtkArrayLog.ContainsKey((arrayType, arrayIndex)):
                Print($"Unknown NumberArray #{arrayIndex} subscribed by {thisPtr->NameString}");
                _subscribeAtkArrayLog.TryAdd((arrayType, arrayIndex), true);
                break;
        }

        _subscribeAtkArrayDataHook!.Original(thisPtr, arrayType, arrayIndex);
    }

    private void DispatchEventDetour(EventHandler* thisPtr, GameObject* gameObject, EventType eventType, uint eventParam)
    {
        if (!Enum.IsDefined(typeof(EventType), (byte)eventType) && !_dispatchEventLog.ContainsKey(eventType))
        {
            var id = thisPtr->GetEventId();

            if (gameObject != null)
            {
                if (gameObject->EntityId == Control.Instance()->LocalPlayerEntityId)
                    Print($"EventHandler {id.ContentId}#{id.EntryId} DispatchEvent(LocalPlayer, {eventType}, {eventParam})");
                else
                    Print($"EventHandler {id.ContentId}#{id.EntryId} DispatchEvent({gameObject->EntityId:X}, {eventType}, {eventParam})");
            }
            else
            {
                Print($"EventHandler {id.ContentId}#{id.EntryId} DispatchEvent(null, {eventType}, {eventParam})");
            }

            _dispatchEventLog.TryAdd(eventType, true);
        }

        _dispatchEventHook!.Original(thisPtr, gameObject, eventType, eventParam);
    }

    private static void Print(ReadOnlySeString str)
    {
        using var rssb = new RentedSeStringBuilder();
        Chat.Print(rssb.Builder
            .PushColorType(32)
            .Append("\uE078 ")
            .PopColorType()
            .Append(str)
            .ToReadOnlySeString());
    }
}
