using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ZoneInitLogTab : PacketLogTab<ZoneInitPacket>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.HandleZoneInitPacket>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleZoneInitPacketDetour(uint targetId, ZoneInitPacket* packet, byte a3)
    {
        AddRecord(*packet);
        _hook!.Original(targetId, packet, a3);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleZoneInitPacket>(PacketDispatcher.MemberFunctionPointers.HandleZoneInitPacket, HandleZoneInitPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("ZoneInitTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
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
            _debugRenderer.DrawPointerType(packet);
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
