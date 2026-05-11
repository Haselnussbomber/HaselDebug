using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits;

[RegisterSingleton<IUnlockTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class OutfitsTab : DebugTab, IUnlockTab
{
    private readonly OutfitsTable _table;
    private readonly ExcelService _excelService;

    public override string Title => "Outfits";

    public UnlockProgress GetUnlockProgress()
    {
        if (_table.Rows.Count == 0)
            _table.LoadRows();

        return new UnlockProgress()
        {
            TotalUnlocks = _table.Rows.Count,
            NumUnlocked = _table.Rows.Count(row => OutfitsTable.IsItemInDresser(row.Set)),
        };
    }

    public override void Draw()
    {
        var numCollectedSets = _table.Rows.Count(row => OutfitsTable.IsItemInDresser(row.Set));
        ImGui.Text($"{numCollectedSets} sets collected. {_table.Rows.Count} of {_excelService.GetRowCount<MirageStoreSetItem>()} rows shown");
        _table.Draw();
    }
}
