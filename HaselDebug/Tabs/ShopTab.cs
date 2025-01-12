using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class ShopTab(DebugRenderer DebugRenderer) : DebugTab
{
    [StructLayout(LayoutKind.Explicit)]
    public struct AddonShop
    {
        [FieldOffset(0x238)] public AtkComponentList* List;
    }

    public override void Draw()
    {
        if (!TryGetAddon<AddonShop>("Shop", out var addon))
        {
            ImGui.TextUnformatted("No shop open!");
            return;
        }

        ImGui.TextUnformatted($"ItemCount: {addon->List->GetItemCount()}");

        for (var i = 0; i < addon->List->GetItemCount(); i++)
        {
            var listItemRenderer = &addon->List->ItemRendererList[i];
            ImGui.TextUnformatted($"{i}:");
            ImGui.SameLine();
            DebugRenderer.DrawCopyableText($"{(nint)listItemRenderer:X}");
            ImGui.SameLine();

            if (addon->List->ItemRendererList[i].IsDisabled)
            {
                if (ImGui.Button($"Enable##ListItem{i}_Enable"))
                {
                    addon->List->SetItemDisabledState(i, false);
                }
            }
            else
            {
                if (ImGui.Button($"Disable##ListItem{i}_Disable"))
                {
                    addon->List->SetItemDisabledState(i, true);
                }
            }
            ImGui.SameLine();
            if (!addon->List->ItemRendererList[i].IsHighlighted)
            {
                if (ImGui.Button($"Highlight##ListItem{i}_Highlight"))
                {
                    addon->List->SetItemHighlightedState(i, true);
                }
            }
            else
            {
                if (ImGui.Button($"Unhighlight##ListItem{i}_Unhighlight"))
                {
                    addon->List->SetItemHighlightedState(i, false);
                }
            }
            ImGui.SameLine();
            if (addon->List->SelectedItemIndex != i)
            {
                if (ImGui.Button($"Select##ListItem{i}_Select"))
                {
                    addon->List->SelectItem(i, false);
                }
                ImGui.SameLine();
                if (ImGui.Button($"Select (Event)##ListItem{i}_SelectEvent"))
                {
                    addon->List->SelectItem(i, true);
                }
            }
            else
            {
                if (ImGui.Button($"Deselect##ListItem{i}_Deselect"))
                {
                    addon->List->DeselectItem();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button($"Send Event 35##ListItem{i}_SendEvent35"))
            {
                addon->List->DispatchItemEvent(i, (AtkEventType)35);
            }
            ImGui.SameLine();
            if (ImGui.Button($"Send Event 37##ListItem{i}_SendEvent37"))
            {
                addon->List->DispatchItemEvent(i, (AtkEventType)37);
            }
        }
    }
}
