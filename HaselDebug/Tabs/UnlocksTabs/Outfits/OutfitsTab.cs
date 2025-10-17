using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class OutfitsTab(OutfitsTable table) : DebugTab, IUnlockTab
{
    private const float IconSize = 32;

    public override string Title => "Outfits";

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(row => ItemFinderModule.Instance()->GlamourDresserItemIds.Contains(row.RowId)),
        };
    }

    public override void Draw()
    {
        var numCollectedSets = table.Rows.Count(row => ItemFinderModule.Instance()->GlamourDresserItemIds.Contains(row.RowId));
        ImGui.Text($"{numCollectedSets} out of {table.Rows.Count} filtered sets collected");

        table.Draw();
    }
}
