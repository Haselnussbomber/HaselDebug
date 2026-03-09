using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class AddonEventsObserverWindow : SimpleWindow
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly DebugRenderer _debugRenderer;
    private readonly List<(DateTime Time, int EventParam, AtkEventType EventType, nint Event, nint EventData)> _events = [];

    private record RefreshEntry(DateTime Time, Pointer<AtkValue> Values, uint ValueCount);

    public string AddonName
    {
        get;
        set { field = value; WindowName = value + " - Events Observer"; }
    }

    public override void Dispose()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, AddonName, OnEvent);
        Clear();
        base.Dispose();
    }

    public override void OnOpen()
    {
        base.OnOpen();

        Size = new Vector2(900, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(250, 250),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Appearing;

        Flags |= ImGuiWindowFlags.NoSavedSettings;

        RespectCloseHotkey = true;
        DisableWindowSounds = true;

        _addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, AddonName, OnEvent);
    }

    public override void OnClose()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, AddonName, OnEvent);
        Clear();
        base.OnClose();
    }

    private unsafe void OnEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs addonSetupArgs)
            return;

        var evt = (AtkEvent*)Marshal.AllocHGlobal(sizeof(AtkEvent));
        *evt = *(AtkEvent*)addonSetupArgs.AtkEvent;

        var data = (AtkEventData*)Marshal.AllocHGlobal(sizeof(AtkEventData));
        *data = *(AtkEventData*)addonSetupArgs.AtkEventData;

        _events.Add((DateTime.Now, addonSetupArgs.EventParam, (AtkEventType)addonSetupArgs.AtkEventType, (nint)evt, (nint)data));
    }

    private void Clear()
    {
        foreach (var (_, _, _, Event, EventData) in _events)
        {
            Marshal.FreeHGlobal(Event);
            Marshal.FreeHGlobal(EventData);
        }

        _events.Clear();
    }

    public override unsafe void Draw()
    {
        using (ImRaii.Disabled(_events.Count == 0))
        {
            if (ImGui.Button("Clear"u8))
            {
                Clear();
            }
        }

        using var table = ImRaii.Table($"{AddonName}EventValuesTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Target"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Listener"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EventParam"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("EventType"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("EventData"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _events.Count - 1; i >= 0; i--)
        {
            var (time, eventParam, eventType, evt, eventData) = _events[i];

            var atkEvent = (AtkEvent*)evt;
            var target = atkEvent->Target;
            var listener = atkEvent->Listener;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Time
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn(); // Target
            _debugRenderer.DrawAddress(target);

            ImGui.TableNextColumn(); // Listener
            if (TryGetAddon<AtkUnitBase>(AddonName, out var addon) && addon == listener)
                ImGui.Text("UnitBase"u8);
            else
                _debugRenderer.DrawAddress(listener);

            ImGui.TableNextColumn(); // EventParam
            ImGui.Text(eventParam.ToString());

            ImGui.TableNextColumn(); // EventType
            ImGui.Text(eventType.ToString() + (Enum.GetName(eventType) != null ? $" ({(int)eventType})" : string.Empty));

            ImGui.TableNextColumn(); // EventData
            var type = eventType.GetAtkEventDataType();
            _debugRenderer.DrawPointerType(eventData, type, new NodeOptions() { AddressPath = new(i) });
        }
    }
}
