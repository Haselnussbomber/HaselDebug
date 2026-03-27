namespace HaselDebug.Interfaces;

public interface IPacketLogTab : IDebugTab
{
    bool IsPacketLogEnabled { get; }

    void Clear();

    void EnablePacketLog();

    void DisablePacketLog();

    void TogglePacketLog();
}
