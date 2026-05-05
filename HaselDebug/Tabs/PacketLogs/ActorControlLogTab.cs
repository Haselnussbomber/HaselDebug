using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using static HaselDebug.Tabs.PacketLogs.ActorControlLogTab;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ActorControlLogTab : PacketLogTab<ActorControlEntry>, IDisposable
{
    private Hook<PacketDispatcher.Delegates.HandleActorControlPacket>? _hook;

    [StructLayout(LayoutKind.Sequential)]
    public struct ActorControlEntry
    {
        public uint EntityId;
        public uint Category;
        public uint Arg1;
        public uint Arg2;
        public uint Arg3;
        public uint Arg4;
    }

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleActorControlPacketDetour(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, GameObjectId targetId, bool isRecorded)
    {
        if (!isRecorded)
        {
            AddRecord(new ActorControlEntry()
            {
                EntityId = entityId,
                Category = category,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4,
            });
        }

        _hook!.Original(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleActorControlPacket>(PacketDispatcher.MemberFunctionPointers.HandleActorControlPacket, HandleActorControlPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("ActorControlLogTable"u8, 7, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EntityId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Category"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg1"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg2"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg3"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg4"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, payload) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(payload.Value->EntityId.ToString("X"));

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Category, typeof(uint), new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg1, typeof(uint), new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg2, typeof(uint), new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg3, typeof(uint), new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg4, typeof(uint), new NodeOptions() { HexOnShift = true });
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
