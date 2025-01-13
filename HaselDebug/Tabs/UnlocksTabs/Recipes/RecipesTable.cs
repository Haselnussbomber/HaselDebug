using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Strings;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes;

[RegisterSingleton]
public unsafe class RecipesTable : Table<Recipe>
{
    internal readonly ExcelService _excelService;

    public RecipesTable(
        ExcelService excelService,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base("RecipesTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new CompletedColumn() {
                Label = "Completed",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new ItemCategory() {
                Label = "Item Category",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 275,
            },
            new NameColumn(textService, unlocksTabUtils) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Recipe>()
            .Where(row => row.ItemResult.RowId > 0)
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<Recipe>
    {
        public override string ToName(Recipe row)
            => row.RowId.ToString();

        public override int ToValue(Recipe row)
            => (int)row.RowId;
    }

    private class CompletedColumn : ColumnBool<Recipe>
    {
        public override unsafe bool ToBool(Recipe row)
            => row.RowId < 30000 && QuestManager.IsRecipeComplete(row.RowId);

        public override void DrawColumn(Recipe row)
        {
            if (row.RowId < 30000)
                base.DrawColumn(row);
        }
    }

    private class ItemCategory : ColumnString<Recipe>
    {
        public override string ToName(Recipe row)
            => row.ItemResult.Value.ItemUICategory.Value.Name.ExtractText().StripSoftHypen();
    }

    private class NameColumn(TextService textService, UnlocksTabUtils unlocksTabUtils) : ColumnString<Recipe>
    {
        public override string ToName(Recipe row)
            => textService.GetItemName(row.ItemResult.RowId);

        public override unsafe void DrawColumn(Recipe row)
        {
            var clicked = unlocksTabUtils.DrawSelectableItem(row.ItemResult.Value!, $"Recipe{row.RowId}");

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (clicked)
                AgentRecipeNote.Instance()->OpenRecipeByRecipeId(row.RowId);
        }
    }
}
