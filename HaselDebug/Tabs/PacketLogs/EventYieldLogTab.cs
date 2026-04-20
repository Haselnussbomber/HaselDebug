using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using EventId = FFXIVClientStructs.FFXIV.Client.Game.Event.EventId;

namespace HaselDebug.Tabs.PacketLogs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x408)]
public partial struct EventYieldRecord
{
    [FieldOffset(0x00)] public EventId EventId;
    [FieldOffset(0x04)] public short Scene;
    [FieldOffset(0x06)] public byte YieldId;
    [FieldOffset(0x07)] public byte IntDataCount;
    [FieldOffset(0x08), FixedSizeArray] internal FixedSizeArray255<int> _intData;
}

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EventYieldLogTab : PacketLogTab<EventYieldRecord>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.HandleEventYieldPacket>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleEventYieldPacketDetour(EventId eventId, short scene, byte yieldId, int* intData, byte intDataCount)
    {
        var record = new EventYieldRecord()
        {
            EventId = eventId,
            Scene = scene,
            YieldId = yieldId,
            IntDataCount = intDataCount,
        };

        new ReadOnlySpan<int>(intData, intDataCount).CopyTo(record.IntData);

        AddRecord(record);

        _hook!.Original(eventId, scene, yieldId, intData, intDataCount);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleEventYieldPacket>(PacketDispatcher.MemberFunctionPointers.HandleEventYieldPacket, HandleEventYieldPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("EventYieldTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EventId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Scene"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("YieldId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Record"u8, ImGuiTableColumnFlags.WidthStretch);
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

            ImGui.TableNextColumn(); // YieldId
            ImGui.Text(record.Value->YieldId.ToString());

            ImGui.TableNextColumn(); // Packet
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
