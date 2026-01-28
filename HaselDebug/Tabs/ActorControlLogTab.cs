using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ActorControlLogTab : DebugTab, IDisposable
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly List<(DateTime Time, uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4)> _records = [];
    private Hook<PacketDispatcher.Delegates.HandleActorControlPacket>? _hook;
    private bool _enabled = false;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void Clear()
    {
        _records.Clear();
    }

    private void HandleActorControlPacketDetour(uint entityId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, GameObjectId targetId, bool isRecorded)
    {
        if (!isRecorded)
            _records.Add((DateTime.Now, entityId, category, arg1, arg2, arg3, arg4));

        _hook!.Original(entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleActorControlPacket>(PacketDispatcher.MemberFunctionPointers.HandleActorControlPacket, HandleActorControlPacketDetour);

        if (ImGui.Checkbox("Enabled", ref _enabled))
        {
            if (_enabled && !_hook.IsEnabled)
            {
                _hook.Enable();
            }
            else if (!_enabled && _hook.IsEnabled)
            {
                _hook.Disable();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnNpcTable"u8, 7, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
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

        for (var i = _records.Count - 1; i >= 0; i--)
        {
            var (time, entityId, category, arg1, arg2, arg3, arg4) = _records[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(entityId.ToString("X"));

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(category.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(arg1.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(arg2.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(arg3.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(arg4.ToString());
        }
    }
}
