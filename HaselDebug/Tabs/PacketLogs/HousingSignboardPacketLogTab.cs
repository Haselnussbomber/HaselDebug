using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class HousingSignboardPacketLogTab : PacketLogTab<HousingSignboardPacket>
{
    private Hook<AgentHousingSignboard.Delegates.ReadPacket>? _hook;

    public override ValueTask DisposeAsync()
    {
        Clear();
        return new ValueTask(_framework.Run(() => DisposeAndNull(ref _hook)));
    }

    private void ReadPacketDetour(AgentHousingSignboard* thisPtr, HousingSignboardPacket* packet)
    {
        AddRecord(*packet);
        _hook!.Original(thisPtr, packet);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<AgentHousingSignboard.Delegates.ReadPacket>(
            AgentHousingSignboard.MemberFunctionPointers.ReadPacket,
            ReadPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("HousingSignboardPacketLogTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
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
