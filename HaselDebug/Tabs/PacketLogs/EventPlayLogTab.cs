using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using EventId = FFXIVClientStructs.FFXIV.Client.Game.Event.EventId;

namespace HaselDebug.Tabs.PacketLogs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x417)]
public partial struct EventPlayRecord
{
    [FieldOffset(0x00)] public GameObjectId ObjectId;
    [FieldOffset(0x08)] public EventId EventId;
    [FieldOffset(0x0C)] public short Scene;
    [FieldOffset(0x0E)] public SceneFlag SceneFlags;
    [FieldOffset(0x16)] public byte SceneDataCount;
    [FieldOffset(0x17), FixedSizeArray] internal FixedSizeArray255<uint> _sceneData;
}

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EventPlayLogTab : PacketLogTab<EventPlayRecord>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.HandleEventPlayPacket>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleEventPlayPacketDetour(GameObjectId objectId, EventId eventId, short scene, ulong sceneFlags, uint* sceneData, byte sceneDataCount)
    {
        var record = new EventPlayRecord()
        {
            ObjectId = objectId,
            EventId = eventId,
            Scene = scene,
            SceneFlags = (SceneFlag)sceneFlags,
            SceneDataCount = sceneDataCount,
        };

        new ReadOnlySpan<uint>(sceneData, sceneDataCount).CopyTo(record.SceneData);

        AddRecord(record);

        _hook!.Original(objectId, eventId, scene, sceneFlags, sceneData, sceneDataCount);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleEventPlayPacket>(PacketDispatcher.MemberFunctionPointers.HandleEventPlayPacket, HandleEventPlayPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("EventPlayTable"u8, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
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
