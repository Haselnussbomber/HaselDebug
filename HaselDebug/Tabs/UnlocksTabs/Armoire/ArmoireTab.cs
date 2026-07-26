using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Armoire;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append)]
public class ArmoireTab(ArmoireTable table) : DebugTab, IUnlockTab
{
    public override string Title => "Armoire";

    public UnlockProgress GetUnlockProgress()
    {
        if (table.Rows.Count == 0)
            table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = table.Rows.Count,
            NumUnlocked = table.Rows.Count(ArmoireTable.IsItemInCabinet),
        };
    }

    public override void Draw()
    {
        var numCollectedSets = table.Rows.Count(ArmoireTable.IsItemInCabinet);
        ImGui.Text($"{numCollectedSets} out of {table.Rows.Count} filtered items collected");
        table.Draw();
    }
}
