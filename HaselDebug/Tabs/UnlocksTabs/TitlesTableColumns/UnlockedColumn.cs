using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TitlesTableColumns;

public class UnlockedColumn : ColumnBool<Title>
{
    public override unsafe bool ToBool(Title row)
    {
        var uiState = UIState.Instance();
        return uiState->TitleList.IsTitleUnlocked((ushort)row.RowId);
    }
}
