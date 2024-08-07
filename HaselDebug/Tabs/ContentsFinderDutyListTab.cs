using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class ContentsFinderDutyListTab : DebugTab
{
    public override void Draw()
    {
        if (!TryGetAddon<AddonContentsFinder>("ContentsFinder", out var addon))
        {
            ImGui.TextUnformatted("ContentsFinder not open");
            if (ImGui.Button("Open"))
            {
                UIModule.Instance()->ExecuteMainCommand(33);
            }
            return;
        }

        ImGui.TextUnformatted($"ItemCount: {addon->DutyList->Items.LongCount}");
        ImGui.TextUnformatted($"SelectedItemIndex: {addon->DutyList->AtkComponentList.SelectedItemIndex}");

        if (ImGui.Button("Deselect"))
        {
            addon->DutyList->DeselectItem();
        }

        // TODO: should probably move this to DebugUtils
        for (var i = 0u; i < addon->DutyList->Items.LongCount; i++)
        {
            var item = addon->DutyList->Items[i].Value;
            ImGui.TextUnformatted($"{i}:");
            ImGui.SameLine();
            DebugUtils.DrawCopyableText($"{(nint)item:X}");

            using (ImRaii.PushIndent())
            {
                ImGui.TextUnformatted("Strings:");
                using (ImRaii.PushIndent())
                {
                    var j = 0;
                    foreach (var stringValue in item->StringValues)
                    {
                        if ((nint)stringValue.Value != 0)
                            ImGui.TextUnformatted($"{j}: {MemoryHelper.ReadStringNullTerminated((nint)stringValue.Value)}");
                        j++;
                    }
                }

                ImGui.TextUnformatted("UInts:");
                using (ImRaii.PushIndent())
                {
                    var j = 0;
                    foreach (var uintValue in item->UIntValues)
                    {
                        ImGui.TextUnformatted($"{j}: {uintValue}");
                        if (j == 0)
                        {
                            ImGui.SameLine();
                            ImGui.TextUnformatted($"({(AtkComponentTreeListItemType)uintValue})");
                        }
                        j++;
                    }
                }
            }
        }
    }
}
