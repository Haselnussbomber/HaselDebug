using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Abstracts;

[AutoConstruct]
public partial class PacketLogTab<T> : DebugTab, IPacketLogTab where T : unmanaged
{
    protected readonly DebugRenderer _debugRenderer;
    protected readonly IGameInteropProvider _gameInteropProvider;

    [StructLayout(LayoutKind.Sequential)]
    private struct RecordEntry
    {
        public DateTime Time;
        public Pointer<T> Payload;
    }

    private readonly List<RecordEntry> _records = [];

    public bool IsPacketLogEnabled { get; protected set; }

    public virtual void Clear()
    {
        _records.Clear();
    }

    public virtual void EnablePacketLog() { }

    public virtual void DisablePacketLog() { }

    public virtual void TogglePacketLog()
    {
        if (IsPacketLogEnabled)
        {
            DisablePacketLog();
        }
        else
        {
            EnablePacketLog();
        }
    }

    public IEnumerable<(int Index, DateTime Time, Pointer<T> Entry)> Records
    {
        get
        {
            for (var i = _records.Count - 1; i >= 0; i--)
            {
                var entry = _records[i];

                unsafe
                {
                    if (entry.Payload.Value == null)
                        continue;
                }

                yield return (i, entry.Time, entry.Payload);
            }
        }
    }

    public virtual unsafe void AddRecord(T payload)
    {
        var copy = IMemorySpace.GetDefaultSpace()->Malloc<T>();
        *copy = payload;
        _records.Add(new RecordEntry() {
            Time = DateTime.Now,
            Payload = copy
        });
    }
}
