using HaselDebug.Tabs;

namespace HaselDebug.Interfaces;

public interface IUnlockTab : ISubTab<UnlocksTab>
{
    public UnlockProgress GetUnlockProgress();
}

public struct UnlockProgress
{
    public bool NeedsExtraData;
    public bool HasExtraData;
    public int TotalUnlocks;
    public int NumUnlocked;
}
