namespace HaselDebug.Tabs;
/*
public unsafe class DropDownListTest : DebugTab
{
    public override void Draw()
    {
        if (!TryGetAddon<AddonCharacterTitle>("CharacterTitle", out var addon))
            return;

        var dropdown = addon->DropDownList;

        ImGui.TextUnformatted($"ItemCount: {dropdown->List->GetItemCount()}");
        ImGui.TextUnformatted($"SelectedItemIndex: {dropdown->List->SelectedItemIndex}");
        if (dropdown->List->SelectedItemIndex != -1)
        {
            ImGui.SameLine();
            if (ImGui.Button($"Deselect##DropdownDeselect"))
            {
                dropdown->DeselectItem();
            }
        }

        for (var i = 0; i < dropdown->List->GetItemCount(); i++)
        {
            var listItemRenderer = dropdown->List->GetListItemRenderer(i);
            ImGui.TextUnformatted($"{i}: {MemoryHelper.ReadSeStringNullTerminated((nint)dropdown->List->GetItemPreviewText(i))}");

            if (dropdown->List->IsItemDisabled(i))
            {
                if (ImGui.Button($"Enable##ListItem{i}_Enable"))
                {
                    dropdown->List->SetItemDisabledState(i, false);
                }
            }
            else
            {
                if (ImGui.Button($"Disable##ListItem{i}_Disable"))
                {
                    dropdown->List->SetItemDisabledState(i, true);
                }
            }
            ImGui.SameLine();
            if (!dropdown->List->IsItemHighlighted(i))
            {
                if (ImGui.Button($"Highlight##ListItem{i}_Highlight"))
                {
                    dropdown->List->SetItemHighlightedState(i, true);
                }
            }
            else
            {
                if (ImGui.Button($"Unhighlight##ListItem{i}_Unhighlight"))
                {
                    dropdown->List->SetItemHighlightedState(i, false);
                }
            }
            ImGui.SameLine();
            if (dropdown->List->SelectedItemIndex != i)
            {
                if (ImGui.Button($"Select##ListItem{i}_Select"))
                {
                    dropdown->List->SelectItem(i, false);
                }
                ImGui.SameLine();
                if (ImGui.Button($"Select (Event)##ListItem{i}_SelectEvent"))
                {
                    dropdown->List->SelectItem(i, true);
                }
            }
            else
            {
                if (ImGui.Button($"Deselect##ListItem{i}_Deselect"))
                {
                    dropdown->List->DeselectItem();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button($"Dropdown Select##ListItem{i}_Select"))
            {
                dropdown->SelectItem(i);
            }
        }
    }
}
*/
