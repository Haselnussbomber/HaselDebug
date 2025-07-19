using System.Collections.Generic;
using Dalamud.Hooking;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AtkEventsTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly List<(DateTime, nint)> _events = [];
    private Hook<AtkEventDispatcher.Delegates.DispatchEvent>? _dispatchEventHook;
    private bool _enabled = false;
    private bool _isInitialized;

    private void Initialize()
    {
        _dispatchEventHook = _gameInteropProvider.HookFromAddress<AtkEventDispatcher.Delegates.DispatchEvent>(AtkEventDispatcher.MemberFunctionPointers.DispatchEvent, DispatchEventDetour);
        //_dispatchEventHook?.Enable();
    }

    public void Dispose()
    {
        foreach (var e in _events)
            Marshal.FreeHGlobal(e.Item2);

        _events.Clear();

        _dispatchEventHook?.Dispose();
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
            AtkEventType.TimerTick or (AtkEventType)74 or (AtkEventType)79))
        {
            var ptr = (AtkEventDispatcher.Event*)Marshal.AllocHGlobal(sizeof(AtkEventDispatcher.Event));
            *ptr = *evt;
            _events.Add((DateTime.Now, (nint)ptr));
        }

        return _dispatchEventHook!.Original(thisPtr, evt);
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        if (_dispatchEventHook == null)
        {
            ImGui.TextUnformatted("Hook not created");
            return;
        }

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
        if (ImGui.Button("Clear")) _events.Clear();

        using var table = ImRaii.Table("EventTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("EventData", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _events.Count - 1; i >= 0; i--)
        {
            var (time, evt) = _events[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(time.ToLongTimeString());

            ImGui.TableNextColumn();
            var eventType = ((AtkEventDispatcher.Event*)evt)->State.EventType;
            ImGui.TextUnformatted(eventType.ToString() + (Enum.GetName(eventType) != null ? $" ({(int)eventType})" : string.Empty));

            ImGui.TableNextColumn();

            if ((int)eventType is >= (int)AtkEventType.MouseDown and <= (int)AtkEventType.MouseDoubleClick)
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(AtkEventData.AtkMouseData), new NodeOptions() { AddressPath = new(i) });
            }
            else if ((int)eventType is >= (int)AtkEventType.InputReceived and <= (int)AtkEventType.InputNavigation)
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(AtkEventData.AtkInputData), new NodeOptions() { AddressPath = new(i) });
            }
            else if ((int)eventType is >= (int)AtkEventType.ListItemRollOver and <= (int)AtkEventType.ListItemSelect)
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(AtkEventData.AtkListItemData), new NodeOptions() { AddressPath = new(i) });
            }
            else if ((int)eventType is >= (int)AtkEventType.DragDropBegin and <= (int)AtkEventType.DragDropCancel)
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(AtkEventData.AtkDragDropData), new NodeOptions() { AddressPath = new(i) });
            }
            else if (eventType == AtkEventType.ChildAddonAttached)
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(AtkEventData.AtkAddonControlData), new NodeOptions() { AddressPath = new(i) });
            }
            else if ((int)eventType is >= (int)AtkEventType.LinkMouseClick and <= (int)AtkEventType.LinkMouseOut)
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(LinkData), new NodeOptions() { AddressPath = new(i) });
            }
            else
            {
                _debugRenderer.DrawPointerType(evt + 0x8, typeof(AtkEventData), new NodeOptions() { AddressPath = new(i) });
            }
        }
    }
}
