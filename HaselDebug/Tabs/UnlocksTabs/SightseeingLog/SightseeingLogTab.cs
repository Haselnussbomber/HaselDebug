using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class SightseeingLogTab : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Sightseeing Log";

    public override void Draw()
    {

    }
}
