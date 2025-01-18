using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Bardings;

[RegisterSingleton]
public unsafe class BardingsTable : Table<BuddyEquip>
{
    internal readonly ExcelService _excelService;

    public BardingsTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        LanguageProvider languageProvider) : base("BardingsTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            RowIdColumn<BuddyEquip>.Create(),
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new ItemColumn(debugRenderer) {
                Label = "Items",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<BuddyEquip>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty)
            .ToList();
    }

    private class UnlockedColumn : ColumnBool<BuddyEquip>
    {
        public override unsafe bool ToBool(BuddyEquip row)
            => UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(row.RowId);
    }

    private class ItemColumn(DebugRenderer debugRenderer) : ColumnString<BuddyEquip>
    {
        public override string ToName(BuddyEquip row)
            => row.Name.ExtractText();

        public override unsafe void DrawColumn(BuddyEquip row)
        {
            debugRenderer.DrawIcon(row.IconBody != 0
                ? row.IconBody
                : row.IconHead != 0
                    ? row.IconHead
                    : row.IconLegs);
            ImGui.TextUnformatted(row.Name.ExtractText());
        }
    }
}
