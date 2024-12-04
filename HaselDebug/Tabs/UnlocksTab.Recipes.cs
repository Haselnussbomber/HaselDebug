using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    private Recipe[]? Recipes;

    public void DrawRecipes()
    {
        using var tab = ImRaii.TabItem("Recipes");
        if (!tab) return;

        using var table = ImRaii.Table("RecipesTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        Recipes ??= ExcelService.FindRows<Recipe>(row => row.ItemResult.RowId != 0);

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        ImGuiClip.ClippedDraw(Recipes, DrawRecipeRow, ImGui.GetTextLineHeightWithSpacing());
    }

    private void DrawRecipeRow(Recipe row)
    {
        if (row.ItemResult.RowId <= 0)
            return;

        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // Id
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn(); // Completed
        if (row.RowId < 30000)
        {
            var isComplete = QuestManager.IsRecipeComplete(row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(QuestManager.IsRecipeComplete(row.RowId) ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isComplete.ToString());
        }

        ImGui.TableNextColumn(); // Name

        var clicked = DrawSelectableItem(row.ItemResult.Value!, $"Recipe{row.RowId}");
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (clicked)
            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(row.RowId);
    }
}
