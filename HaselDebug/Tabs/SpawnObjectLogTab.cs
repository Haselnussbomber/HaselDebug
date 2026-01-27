using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpawnObjectLogTab : DebugTab, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameGui _gameGui;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly List<(DateTime Time, Pointer<SpawnObjectPacket> Packet)> _npcRecords = [];
    private Hook<PacketDispatcher.Delegates.HandleSpawnObjectPacket>? _spawnObjectHook;
    private bool _enabled = false;

    public void Dispose()
    {
        _spawnObjectHook?.Dispose();
        Clear();
    }

    private void Clear()
    {
        foreach (var (_, Packet) in _npcRecords)
            Marshal.FreeHGlobal((nint)Packet.Value);

        _npcRecords.Clear();
    }

    private void HandleSpawnObjectPacketDetour(uint targetId, SpawnObjectPacket* packet)
    {
        var ptr = (SpawnObjectPacket*)Marshal.AllocHGlobal(sizeof(SpawnObjectPacket));
        *ptr = *packet;
        _npcRecords.Add((DateTime.Now, (Pointer<SpawnObjectPacket>)ptr));
        _spawnObjectHook!.Original(targetId, packet);
    }

    public delegate void HandleSpawnObjectPacket(PacketDispatcher* a1, SpawnObjectPacket* packet);

    public override void Draw()
    {
        _spawnObjectHook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleSpawnObjectPacket>(PacketDispatcher.MemberFunctionPointers.HandleSpawnObjectPacket, HandleSpawnObjectPacketDetour);

        if (ImGui.Checkbox("Enabled", ref _enabled))
        {
            if (_enabled && !_spawnObjectHook.IsEnabled)
            {
                _spawnObjectHook.Enable();
            }
            else if (!_enabled && _spawnObjectHook.IsEnabled)
            {
                _spawnObjectHook.Disable();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnObjectTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _npcRecords.Count - 1; i >= 0; i--)
        {
            var (time, packet) = _npcRecords[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();

            var objectKind = packet.Value->ObjectKind;
            var name = $"[{(ObjectKind)objectKind}] ";

            _debugRenderer.DrawPointerType(packet, typeof(SpawnObjectPacket), new NodeOptions()
            {
                AddressPath = new(i),
                Title = name
            });
        }
    }
}
