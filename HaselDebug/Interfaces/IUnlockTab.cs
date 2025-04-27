namespace HaselDebug.Interfaces;

public interface IUnlockTab : IDebugTab
{
    UnlockProgress GetUnlockProgress();
}

public struct UnlockProgress
{
    public bool NeedsExtraData;
    public bool HasExtraData;
    public int TotalUnlocks;
    public int NumUnlocked;
}
