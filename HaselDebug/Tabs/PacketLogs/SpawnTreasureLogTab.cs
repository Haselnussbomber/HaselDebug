using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpawnTreasureLogTab : PacketLogTab<SpawnTreasurePacket>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.HandleSpawnTreasurePacket>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleSpawnTreasurePacketDetour(uint targetId, SpawnTreasurePacket* packet)
    {
        AddRecord(*packet);
        _hook!.Original(targetId, packet);
    }

    public delegate void HandleSpawnTreasurePacket(PacketDispatcher* a1, SpawnTreasurePacket* packet);

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleSpawnTreasurePacket>(PacketDispatcher.MemberFunctionPointers.HandleSpawnTreasurePacket, HandleSpawnTreasurePacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnTreasureTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, packet) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            _debugRenderer.DrawPointerType((SpawnTreasurePacket*)Unsafe.AsPointer(in packet), new NodeOptions()
            {
                AddressPath = new(index)
            });
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
