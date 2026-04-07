using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpawnObjectLogTab : PacketLogTab<SpawnObjectPacket>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.HandleSpawnObjectPacket>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleSpawnObjectPacketDetour(uint targetId, SpawnObjectPacket* packet)
    {
        AddRecord(*packet);
        _hook!.Original(targetId, packet);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleSpawnObjectPacket>(PacketDispatcher.MemberFunctionPointers.HandleSpawnObjectPacket, HandleSpawnObjectPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnObjectTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (i, time, packet) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            var objectKind = packet.Value->ObjectKind;
            var name = $"[{(ObjectKind)objectKind}] ";
            _debugRenderer.DrawPointerType(packet, new NodeOptions() { Title = name });
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
