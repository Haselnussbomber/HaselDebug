using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AtkEventsTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly List<(DateTime, nint)> _events = [];
    private Hook<AtkEventDispatcher.Delegates.DispatchEvent>? _dispatchEventHook;
    private bool _enabled = false;

    public void Dispose()
    {
        _dispatchEventHook?.Dispose();
        Clear();
    }

    private void Clear()
    {
        foreach (var e in _events)
            Marshal.FreeHGlobal(e.Item2);

        _events.Clear();
    }

    private bool DispatchEventDetour(AtkEventDispatcher* thisPtr, AtkEventDispatcher.Event* evt)
    {
        if (_enabled && evt != null && evt->State.EventType is not (
            //AtkEventType.MouseUp or
            //AtkEventType.MouseDown or
            AtkEventType.MouseMove or
            AtkEventType.MouseOver or
            AtkEventType.MouseOut or
            AtkEventType.FocusStart or
            AtkEventType.FocusStop or
            AtkEventType.WindowRollOver or
            AtkEventType.WindowRollOut or
            AtkEventType.TimerTick or
            AtkEventType.TimelineActiveLabelChanged or
            (AtkEventType)79))
        {
            var ptr = (AtkEventDispatcher.Event*)Marshal.AllocHGlobal(sizeof(AtkEventDispatcher.Event));
            *ptr = *evt;
            _events.Add((DateTime.Now, (nint)ptr));
        }

        return _dispatchEventHook!.Original(thisPtr, evt);
    }

    public override void Draw()
    {
        _dispatchEventHook ??= _gameInteropProvider.HookFromAddress<AtkEventDispatcher.Delegates.DispatchEvent>(AtkEventDispatcher.MemberFunctionPointers.DispatchEvent, DispatchEventDetour);

        if (ImGui.Checkbox("Enabled", ref _enabled))
        {
            if (_enabled && !_dispatchEventHook.IsEnabled)
            {
                _dispatchEventHook.Enable();
            }
            else if (!_enabled && _dispatchEventHook.IsEnabled)
            {
                _dispatchEventHook.Disable();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("EventTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("EventData"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _events.Count - 1; i >= 0; i--)
        {
            var (time, evt) = _events[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            var eventType = ((AtkEventDispatcher.Event*)evt)->State.EventType;
            ImGui.Text(eventType.ToString() + (Enum.GetName(eventType) != null ? $" ({(int)eventType})" : string.Empty));

            ImGui.TableNextColumn();

            var type = typeof(AtkEventData);

            if ((int)eventType is >= (int)AtkEventType.MouseDown and <= (int)AtkEventType.MouseDoubleClick)
            {
                type = typeof(AtkEventData.AtkMouseData);
            }
            else if ((int)eventType is >= (int)AtkEventType.InputReceived and <= (int)AtkEventType.InputNavigation)
            {
                type = typeof(AtkEventData.AtkInputData);
            }
            else if ((int)eventType is >= (int)AtkEventType.ListItemRollOver and <= (int)AtkEventType.ListItemSelect)
            {
                type = typeof(AtkEventData.AtkListItemData);
            }
            else if ((int)eventType is >= (int)AtkEventType.DragDropBegin and <= (int)AtkEventType.DragDropClick)
            {
                type = typeof(AtkEventData.AtkDragDropData);
            }
            else if (eventType == AtkEventType.ChildAddonAttached)
            {
                type = typeof(AtkEventData.AtkAddonControlData);
            }
            else if (eventType == AtkEventType.ValueUpdate)
            {
                type = typeof(AtkEventData.AtkValueData);
            }
            else if (eventType == AtkEventType.TimelineActiveLabelChanged)
            {
                type = typeof(AtkEventData.AtkTimelineData);
            }
            else if ((int)eventType is >= (int)AtkEventType.LinkMouseClick and <= (int)AtkEventType.LinkMouseOut)
            {
                type = typeof(LinkData);
            }

            _debugRenderer.DrawPointerType(evt + 0x8, type, new NodeOptions() { AddressPath = new(i) });
        }
    }
}
