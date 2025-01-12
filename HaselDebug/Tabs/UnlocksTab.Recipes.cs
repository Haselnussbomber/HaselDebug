using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabRecipes(ExcelService ExcelService, UnlocksTabUtils UnlocksTabUtils) : DebugTab, ISubTab<UnlocksTab>
{
    private Recipe[]? _recipes;

    public override string Title => "Recipes";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("RecipesTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        _recipes ??= ExcelService.FindRows<Recipe>(row => row.ItemResult.RowId != 0);

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        ImGuiClip.ClippedDraw(_recipes, DrawRecipeRow, ImGui.GetTextLineHeightWithSpacing());
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

        var clicked = UnlocksTabUtils.DrawSelectableItem(row.ItemResult.Value!, $"Recipe{row.RowId}");
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (clicked)
            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(row.RowId);
    }
}
