using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using EventId = FFXIVClientStructs.FFXIV.Client.Game.Event.EventId;

namespace HaselDebug.Tabs.PacketLogs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x404)]
public partial struct EventCompleteRecord
{
    [FieldOffset(0x00)] public EventId EventId;
    [FieldOffset(0x04)] public short Scene;
    [FieldOffset(0x06)] public byte a3;
    [FieldOffset(0x07)] public byte PayloadSize;
    [FieldOffset(0x08), FixedSizeArray] internal FixedSizeArray255<uint> _payload;
}

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EventCompleteLogTab : PacketLogTab<EventCompleteRecord>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.SendEventCompletePacket>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void SendEventCompletePacketDetour(EventId eventId, short scene, byte a3, uint* payload, byte payloadSize, void* a6)
    {
        var record = new EventCompleteRecord()
        {
            EventId = eventId,
            Scene = scene,
            a3 = a3,
            PayloadSize = payloadSize,
        };

        new ReadOnlySpan<uint>(payload, payloadSize).CopyTo(record.Payload);

        AddRecord(record);

        _hook!.Original(eventId, scene, a3, payload, payloadSize, a6);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.SendEventCompletePacket>(PacketDispatcher.MemberFunctionPointers.SendEventCompletePacket, SendEventCompletePacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("EventCompleteTable"u8, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EventId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Scene"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (i, time, record) in Records)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Time
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn(); // EventId
            ImGui.Text(record.Value->EventId.Id.ToString("X"));

            ImGui.TableNextColumn(); // Scene
            ImGui.Text(record.Value->Scene.ToString());

            ImGui.TableNextColumn(); // Record
            _debugRenderer.DrawPointerType(record);
        }
    }

    public override void EnablePacketLog()
    {
        _hook!.Enable();
        IsPacketLogEnabled = _hook.IsEnabled;
    }

    public override void DisablePacketLog()
    {
        _hook!.Disable();
        IsPacketLogEnabled = _hook.IsEnabled;
    }
}
