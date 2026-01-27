using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpawnTreasureLogTab : DebugTab, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameGui _gameGui;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly List<(DateTime Time, Pointer<SpawnTreasurePacket> Packet)> _npcRecords = [];
    private Hook<PacketDispatcher.Delegates.HandleSpawnTreasurePacket>? _spawnTreasureHook;
    private bool _enabled = false;

    public void Dispose()
    {
        _spawnTreasureHook?.Dispose();
        Clear();
    }

    private void Clear()
    {
        foreach (var (_, Packet) in _npcRecords)
            Marshal.FreeHGlobal((nint)Packet.Value);

        _npcRecords.Clear();
    }

    private void HandleSpawnTreasurePacketDetour(uint targetId, SpawnTreasurePacket* packet)
    {
        var ptr = (SpawnTreasurePacket*)Marshal.AllocHGlobal(sizeof(SpawnTreasurePacket));
        *ptr = *packet;
        _npcRecords.Add((DateTime.Now, (Pointer<SpawnTreasurePacket>)ptr));
        _spawnTreasureHook!.Original(targetId, packet);
    }

    public delegate void HandleSpawnTreasurePacket(PacketDispatcher* a1, SpawnTreasurePacket* packet);

    public override void Draw()
    {
        _spawnTreasureHook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleSpawnTreasurePacket>(PacketDispatcher.MemberFunctionPointers.HandleSpawnTreasurePacket, HandleSpawnTreasurePacketDetour);

        if (ImGui.Checkbox("Enabled", ref _enabled))
        {
            if (_enabled && !_spawnTreasureHook.IsEnabled)
            {
                _spawnTreasureHook.Enable();
            }
            else if (!_enabled && _spawnTreasureHook.IsEnabled)
            {
                _spawnTreasureHook.Disable();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnTreasureTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
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
            _debugRenderer.DrawPointerType(packet, typeof(SpawnTreasurePacket), new NodeOptions()
            {
                AddressPath = new(i)
            });
        }
    }
}
