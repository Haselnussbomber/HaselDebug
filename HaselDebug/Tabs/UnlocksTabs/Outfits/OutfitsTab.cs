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
            NumUnlocked = _table.Rows.Count(_table.IsSetCollected),
        };
    }

    public override void Draw()
    {
        var numCollectedSets = _table.Rows.Count(_table.IsSetCollected);

        ImGui.Text($"{numCollectedSets} sets collected. {_table.Rows.Count} of {_excelService.GetRowCount<MirageStoreSetItem>()} rows shown");

        if (ImGui.Checkbox("Show only sets that have items which can be stored in the Armoire"u8, ref _table.ArmoireOnly))
        {
            _table.LoadRows();
            _table.IsFilterDirty = true;
        }

        _table.Draw();
    }
}
