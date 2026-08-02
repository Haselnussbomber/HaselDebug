using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ServerRequestCallbackLogTab : DebugTab, IPacketLogTab, IDisposable
{
    protected readonly DebugRenderer _debugRenderer;
    protected readonly IGameInteropProvider _gameInteropProvider;

    private Hook<ServerRequestCallbackManager.Delegates.ProcessPacket>? _hook;
    private readonly List<ServerRequestCallbackEntry> _records = [];

    public bool IsPacketLogEnabled { get; private set; }

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void ProcessPacketDetour(ServerRequestCallbackManager* thisPtr, int callbackIndex, int commandId, void* payload, nuint payloadSize)
    {
        var payloadCopy = IMemorySpace.GetDefaultSpace()->Malloc(payloadSize, 8);
        Buffer.MemoryCopy(payload, payloadCopy, payloadSize, payloadSize);
        _records.Add(new ServerRequestCallbackEntry
        {
            Time = DateTime.Now,
            CommandId = commandId,
            Payload = payloadCopy,
            PayloadSize = payloadSize
        });
        _hook!.Original(thisPtr, callbackIndex, commandId, payload, payloadSize);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<ServerRequestCallbackManager.Delegates.ProcessPacket>(ServerRequestCallbackManager.MemberFunctionPointers.ProcessPacket, ProcessPacketDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("ServerRequestCallbackLogTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("CommandId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Data"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var record in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(record.Time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGui.Text(record.CommandId.ToString());

            ImGui.TableNextColumn();
            _debugRenderer.DrawHexView((nint)record.Payload, (int)record.PayloadSize, new());
        }
    }

    public void EnablePacketLog()
    {
        _hook!.Enable();
        IsPacketLogEnabled = _hook.IsEnabled;
    }

    public void DisablePacketLog()
    {
        _hook!.Disable();
        IsPacketLogEnabled = _hook.IsEnabled;
    }

    public void TogglePacketLog()
    {
        if (IsPacketLogEnabled)
            DisablePacketLog();
        else
            EnablePacketLog();
    }

    public void Clear()
    {
        foreach (var record in _records)
            record.Dispose();
        _records.Clear();
    }

    public IEnumerable<ServerRequestCallbackEntry> Records
    {
        get
        {
            for (var i = _records.Count - 1; i >= 0; i--)
            {
                var record = _records[i];

                unsafe
                {
                    if (record.Payload == null)
                        continue;
                }

                yield return record;
            }
        }
    }

    public class ServerRequestCallbackEntry : IDisposable
    {
        public DateTime Time;
        public int CommandId;
        public void* Payload;
        public nuint PayloadSize;

        public void Dispose()
        {
            if (Payload != null)
            {
                IMemorySpace.Free(Payload, 0);
                Payload = null;
            }
        }
    }
}
