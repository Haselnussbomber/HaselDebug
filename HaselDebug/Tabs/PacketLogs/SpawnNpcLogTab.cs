using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using HaselDebug.Windows;
using static HaselDebug.Tabs.PacketLogs.SpawnNpcLogTab;
using DObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SpawnNpcLogTab : PacketLogTab<SpawnNpcEntry>, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;
    private readonly IGameGui _gameGui;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private Hook<PacketDispatcher.Delegates.HandleSpawnNpcPacket>? _hook;

    [StructLayout(LayoutKind.Sequential)]
    public struct SpawnNpcEntry
    {
        public uint EntityId;
        public SpawnNpcPacket Packet;
    }

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void HandleSpawnNpcPacketDetour(uint entityId, SpawnNpcPacket* packet)
    {
        AddRecord(new SpawnNpcEntry()
        {
            EntityId = entityId,
            Packet = *packet,
        });

        _hook!.Original(entityId, packet);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<PacketDispatcher.Delegates.HandleSpawnNpcPacket>(PacketDispatcher.MemberFunctionPointers.HandleSpawnNpcPacket, HandleSpawnNpcPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SpawnNpcTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EntityId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, entry) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(entry.EntityId.ToString("X"));

            ImGui.TableNextColumn();

            var objectKind = entry.Packet.Common.ObjectKind;
            var name = $"[{objectKind}] ";

            if (entry.Packet.Common.NameId != 0)
                name += _seStringEvaluator.EvaluateObjStr((DObjectKind)objectKind, entry.Packet.Common.NameId);
            else
                name += entry.Packet.Common.NameString;

            _debugRenderer.DrawPointerType((SpawnNpcPacket*)Unsafe.AsPointer(in entry.Packet), new NodeOptions()
            {
                AddressPath = new(index),
                Title = name,
                OnHovered = () =>
                {
                    var gameObject = GameObjectManager.Instance()->Objects.GetObjectByEntityId(entry.EntityId);
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
                    var gameObject = GameObjectManager.Instance()->Objects.GetObjectByEntityId(entry.EntityId);
                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        Visible = gameObject != null,
                        ClickCallback = () =>
                        {
                            var windowName = $"Entity #{entry.EntityId:X}";
                            var window = _windowManager.CreateOrOpen(windowName, () => _serviceProvider.CreateInstance<EntityInspectorWindow>());
                            window.WindowNameKey = string.Empty;
                            window.WindowName = windowName;
                            window.EntityId = entry.EntityId;
                        }
                    });
                    builder.AddCopyName(name);
                    builder.AddCopyAddress((nint)gameObject);
                }
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
