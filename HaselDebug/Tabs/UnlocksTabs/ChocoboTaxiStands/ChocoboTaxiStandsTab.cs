using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.ChocoboTaxiStands;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class ChocoboTaxiStandsTab(ChocoboTaxiStandsTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Chocobo Taxi Stands";
    public override bool DrawInChild => false;

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(row => UIState.Instance()->IsChocoboTaxiStandUnlocked(row.RowId - 1179648)),
        };
    }

    public override void Draw()
    {
        table.Draw();
    }
}
