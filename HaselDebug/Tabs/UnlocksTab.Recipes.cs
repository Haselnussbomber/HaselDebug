using System.Linq;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    private Recipe[]? Recipes;

    public void DrawRecipes()
    {
        using var tab = ImRaii.TabItem("Recipes");
        if (!tab) return;

        using var table = ImRaii.Table("RecipesTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY);
        if (!table) return;

        Recipes ??= ExcelService.GetSheet<Recipe>().Where(row => row.ItemResult.Row != 0).ToArray();

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        ImGuiClip.ClippedDraw(Recipes, DrawRecipeRow, ImGui.GetTextLineHeightWithSpacing());
    }

    private void DrawRecipeRow(Recipe row)
    {
        if (row.ItemResult.Row <= 0)
            return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(row.RowId.ToString());

        ImGui.TableNextColumn();
        if (row.RowId < 30000)
        {
            var isComplete = QuestManager.IsRecipeComplete(row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(QuestManager.IsRecipeComplete(row.RowId) ? Colors.Green : Colors.Red)))
                ImGui.TextUnformatted(isComplete.ToString());
        }

        ImGui.TableNextColumn();
        DebugUtils.DrawIcon(TextureProvider, row.ItemResult.Value?.Icon ?? 0u);
        if (ImGui.Selectable(TextService.GetItemName(row.ItemResult.Row)))
            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(row.RowId);
    }
}
