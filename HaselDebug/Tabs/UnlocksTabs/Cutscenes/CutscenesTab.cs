using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Cutscenes;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class CutscenesTab(CutscenesTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Cutscenes";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(entry => UIState.Instance()->IsCutsceneSeen((uint)entry.Index)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
