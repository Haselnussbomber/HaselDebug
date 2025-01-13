using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class SightseeingLogTab(SightseeingLogTable table) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Sightseeing Log";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        table.Draw();
    }
}
