using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ContentsFinderDutyListTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

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

        if (ImGui.Button("12 0"))
        {
            var returnValue = stackalloc AtkValue[1];
            var command = stackalloc AtkValue[2];

            command[0].SetInt(12);
            command[1].SetInt(0);

            AgentContentsFinder.Instance()->ReceiveEvent(returnValue, command, 2, 0);
        }

        if (ImGui.Button("12 1"))
        {
            var returnValue = stackalloc AtkValue[1];
            var command = stackalloc AtkValue[2];

            command[0].SetInt(12);
            command[1].SetInt(1);

            AgentContentsFinder.Instance()->ReceiveEvent(returnValue, command, 2, 0);
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
            _debugRenderer.DrawCopyableText($"{(nint)item:X}");

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
