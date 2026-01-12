using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using DObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpawnNpcLogTab : DebugTab, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameGui _gameGui;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly List<(DateTime Time, uint entityId, Pointer<SpawnNpcPacket> Packet)> _npcRecords = [];
    private Hook<PacketDispatcher.Delegates.HandleSpawnNpcPacket>? _spawnNpcHook;
    private bool _enabled = false;

    public void Dispose()
    {
        _spawnNpcHook?.Dispose();
        Clear();
    }

    private void Clear()
    {
        foreach (var (_, _, Packet) in _npcRecords)
            Marshal.FreeHGlobal((nint)Packet.Value);

        _npcRecords.Clear();
    }

    private void HandleSpawnNpcPacketDetour(uint entityId, SpawnNpcPacket* packet)
    {
        var ptr = (SpawnNpcPacket*)Marshal.AllocHGlobal(sizeof(SpawnNpcPacket));
        *ptr = *packet;
        _npcRecords.Add((DateTime.Now, entityId, (Pointer<SpawnNpcPacket>)ptr));
        _spawnNpcHook!.Original(entityId, packet);
    }

    public override void Draw()
    {
        _spawnNpcHook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleSpawnNpcPacket>(PacketDispatcher.MemberFunctionPointers.HandleSpawnNpcPacket, HandleSpawnNpcPacketDetour);

        if (ImGui.Checkbox("Enabled", ref _enabled))
        {
            if (_enabled && !_spawnNpcHook.IsEnabled)
            {
                _spawnNpcHook.Enable();
            }
            else if (!_enabled && _spawnNpcHook.IsEnabled)
            {
                _spawnNpcHook.Disable();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnNpcTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EntityId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EventData"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _npcRecords.Count - 1; i >= 0; i--)
        {
            var (time, entityId, packet) = _npcRecords[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(entityId.ToString("X"));

            ImGui.TableNextColumn();

            var objectKind = packet.Value->Common.ObjectKind;
            var name = $"[{objectKind}] ";

            if (packet.Value->Common.BNpcNameId != 0)
                name += _seStringEvaluator.EvaluateObjStr((DObjectKind)objectKind, packet.Value->Common.BNpcNameId);
            else
                name += packet.Value->Common.NameString;

            _debugRenderer.DrawPointerType(packet, typeof(SpawnNpcPacket), new NodeOptions()
            {
                AddressPath = new(i),
                Title = name,
                OnHovered = () =>
                {
                    var gameObject = GameObjectManager.Instance()->Objects.GetObjectByEntityId(entityId);
                    if (gameObject != null)
                    {
                        var pos = gameObject->GetPosition();
                        if (pos != null && _gameGui.WorldToScreen(*pos, out var screenPos))
                        {
                            ImGui.GetForegroundDrawList().AddLine(ImGui.GetMousePos(), screenPos, Color.Orange.ToUInt());
                            ImGui.GetForegroundDrawList().AddCircleFilled(screenPos, 3f, Color.Orange.ToUInt());
                        }
                    }
                },
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    var gameObject = GameObjectManager.Instance()->Objects.GetObjectByEntityId(entityId);
                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        Visible = gameObject != null,
                        ClickCallback = () =>
                        {
                            var windowName = $"Entity #{entityId:X}";
                            var window = _windowManager.CreateOrOpen(windowName, () => _serviceProvider.CreateInstance<EntityInspectorWindow>());
                            window.WindowNameKey = string.Empty;
                            window.WindowName = windowName;
                            window.EntityId = entityId;
                        }
                    });
                    builder.AddCopyName(name);
                    builder.AddCopyAddress((nint)gameObject);
                }
            });
        }
    }
}
