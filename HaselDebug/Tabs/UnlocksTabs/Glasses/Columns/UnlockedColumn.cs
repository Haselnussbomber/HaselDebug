using HaselCommon.Gui.ImGuiTable;
using GlassesSheet = Lumina.Excel.Sheets.Glasses;

namespace HaselDebug.Tabs.UnlocksTabs.Glasses.Columns;

[RegisterTransient, AutoConstruct]
public partial class UnlockedColumn : ColumnYesNo<GlassesSheet>
{
    private readonly IUnlockState _unlockState;

    [AutoPostConstruct]
    private void Initialize()
    {
        SetFixedWidth(75);
        LabelKey = "UnlockedColumn.Label";
    }

    public override bool ToBool(GlassesSheet row)
    {
        return _unlockState.IsGlassesUnlocked(row);
    }
}
