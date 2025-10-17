using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<Recipe>
{
    private readonly TextService _textService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(Recipe row)
        => _textService.GetItemName(row.ItemResult.RowId).ToString();

    public override unsafe void DrawColumn(Recipe row)
    {
        var clicked = _unlocksTabUtils.DrawSelectableItem(row.ItemResult.Value!, $"Recipe{row.RowId}");

        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        if (clicked && AgentLobby.Instance()->IsLoggedIn)
            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(row.RowId);
    }
}
